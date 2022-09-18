/*
	FITS Rating Tool
	Copyright (C) 2022 TheCyberBrick

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#pragma once

#include "fitsio.h"

namespace Loader
{
	template<typename T_IN, typename T_OUT>
	struct DataKernel
	{
		// kernel size
		int size = 0;
		// kernel stride
		int stride = 0;

		// weights of kernel,
		// (1 + 2 * size) x (1 + 2 * size) row major matrix
		float* weights = nullptr;

		// offset to be added to each pixel
		T_OUT offset{};

		// whether the input image is RGB
		bool rgb = false;

		// cfa, 3 x 4 row major matrix,
		// rgb must be true for cfa to
		// take effect
		float* cfa = nullptr;
	};

	template<typename T_IN, typename T_OUT>
	struct DataOutput
	{
		bool* negative = nullptr;
		T_OUT* out_data_ptr = nullptr;
	};

	template<typename T_IN, typename T_OUT>
	struct DataIteratorState
	{
		int input_width = 0;
		int input_height = 0;

		int output_width = 0;
		int output_height = 0;

		// input data from cfitsio
		T_IN* in_data_ptr = nullptr;

		// 1 + 2 * kernelSize
		int kernel_dim = 0;

		// iteratorData.kernelDim - 1
		int processing_start = 0;

		// kernelDim * width
		int buffer_size = 0;

		// data buffer that holds bufferSize number of values
		// for the kernel processing
		std::unique_ptr<T_IN[]> buffer_ptr = nullptr;

		// buffer index where processing will start taking place
		int buffer_processing_index = 0;

		// buffer write cursor
		int buffer_index = 0;
		int buffer_row_start_index = 0;

		// counters for current image row and plane
		int input_row_counter = 0;
		int input_plane_counter = 0;

		// current output y coordinate
		int out_data_y = 0;
	};

	template<typename T_IN, typename T_OUT>
	class DataIterator
	{
	public:
		DataIterator(int width, int height, DataKernel<T_IN, T_OUT> kernel, DataOutput<T_IN, T_OUT> output) :
			m_kernel(kernel), m_output(output), m_state()
		{
			m_state.input_width = width;
			m_state.input_height = height;
		}

		int Read(long totaln, long offset, long firstn, long nvalues, int narrays, iteratorCol* data)
		{
			if (firstn == 1)
			{
				// Initialize iterator

				if (narrays != 1)
				{
					return -1;
				}

				if (!m_kernel.rgb)
				{
					m_kernel.cfa = nullptr;
				}

				m_state.output_width = (m_state.input_width / (m_kernel.cfa ? 2 : 1) - 2 * m_kernel.size) / m_kernel.stride;
				m_state.output_height = (m_state.input_height / (m_kernel.cfa ? 2 : 1) - 2 * m_kernel.size) / m_kernel.stride;
				m_state.in_data_ptr = (T_IN*)fits_iter_get_array(&data[0]);
				m_state.kernel_dim = (1 + 2 * m_kernel.size);
				m_state.processing_start = m_state.kernel_dim * (m_kernel.cfa ? 2 : 1) - 1;
				m_state.buffer_size = m_state.kernel_dim * (m_kernel.cfa ? 2 : 1) /*y*/ * m_state.input_width /*x*/;
				m_state.buffer_ptr = std::make_unique<T_IN[]>(m_state.buffer_size);
				m_state.buffer_processing_index = 0;
				m_state.buffer_index = 0;
				m_state.buffer_row_start_index = 0;
				m_state.input_row_counter = 0;
				m_state.input_plane_counter = 0;
				m_state.out_data_y = 0;
			}

			T_OUT* out_data_ptr = m_output.out_data_ptr;
			T_IN* buffer = m_state.buffer_ptr.get();

			// cfitsio data starts at 1. 0th element contains null value
			int in_data_index = 1;

			bool finished = false;
			bool negative = false;

			int const num_planes = (m_kernel.rgb && !m_kernel.cfa) ? 3 : 1;

			int const pixel_stride = m_kernel.stride * (m_kernel.cfa ? 2 : 1);

			// copy data into buffer row by row
			int remaining = nvalues;
			while (remaining > 0)
			{
				// calculate number of values to be copied into buffer until none remain or buffer row becomes full
				int const count = std::min(m_state.buffer_index + remaining, m_state.buffer_row_start_index + m_state.input_width) - m_state.buffer_index;

				// copy data into buffer and advance cursors
				memcpy(buffer + m_state.buffer_index, m_state.in_data_ptr + in_data_index, count * sizeof(T_IN));
				m_state.buffer_index += count;
				in_data_index += count;
				remaining -= count;

				// check if buffer row full
				if (m_state.buffer_index == m_state.buffer_row_start_index + m_state.input_width)
				{
					m_state.buffer_index = m_state.buffer_row_start_index = m_state.buffer_index % m_state.buffer_size;

					// check if buffer is full enough to begin processing
					// and whether values need to be output (Y axis kernel stride)
					if (m_state.input_row_counter >= m_state.processing_start && (m_state.input_row_counter - m_state.processing_start) % pixel_stride == 0)
					{
						float cfa[12];
						if (m_kernel.cfa)
						{
							memcpy(cfa, m_kernel.cfa, sizeof(cfa));
						}

						if (!m_kernel.cfa)
						{
							for (int out_x = 0; out_x < m_state.output_width; out_x++)
							{
								int const kernel_start = out_x * m_kernel.stride;

								// no cfa, can apply kernel directly
								float vsum = 0;
								for (int kernel_y = 0; kernel_y < m_state.kernel_dim; kernel_y++)
								{
									for (int kernel_x = kernel_start; kernel_x < kernel_start + m_state.kernel_dim; kernel_x++)
									{
										T_IN v = buffer[(m_state.buffer_processing_index + (kernel_y * m_state.input_width + kernel_x)) % m_state.buffer_size];
										negative |= v < 0;
										vsum += m_kernel.weights[kernel_y * m_state.kernel_dim + kernel_x - kernel_start] * v;
									}
								}
								out_data_ptr[m_state.out_data_y * m_state.output_width + out_x] = static_cast<T_OUT>(vsum + m_kernel.offset);
							}
						}
						else
						{
							for (int out_x = 0; out_x < m_state.output_width; out_x++)
							{
								int const kernel_start = out_x * m_kernel.stride;

								// need to calculate r, g and b through
								// cfa matrix and then apply kernel
								float rsum = 0;
								float gsum = 0;
								float bsum = 0;
								for (int kernel_y = 0; kernel_y < m_state.kernel_dim; kernel_y++)
								{
									for (int kernel_x = kernel_start; kernel_x < kernel_start + m_state.kernel_dim; kernel_x++)
									{
										float w = m_kernel.weights[kernel_y * m_state.kernel_dim + kernel_x - kernel_start];
										T_IN tl = buffer[(m_state.buffer_processing_index + (kernel_y * m_state.input_width + kernel_x) * 2 + 0 + 0 * m_state.input_width) % m_state.buffer_size];
										T_IN tr = buffer[(m_state.buffer_processing_index + (kernel_y * m_state.input_width + kernel_x) * 2 + 1 + 0 * m_state.input_width) % m_state.buffer_size];
										T_IN bl = buffer[(m_state.buffer_processing_index + (kernel_y * m_state.input_width + kernel_x) * 2 + 0 + 1 * m_state.input_width) % m_state.buffer_size];
										T_IN br = buffer[(m_state.buffer_processing_index + (kernel_y * m_state.input_width + kernel_x) * 2 + 1 + 1 * m_state.input_width) % m_state.buffer_size];
										negative |= tl < 0 || tr < 0 || bl < 0 || br < 0;
										float tlw = w * tl;
										float trw = w * tr;
										float blw = w * bl;
										float brw = w * br;
										rsum += tlw * cfa[0] + trw * cfa[1] + blw * cfa[2] + brw * cfa[3];
										gsum += tlw * cfa[4] + trw * cfa[5] + blw * cfa[6] + brw * cfa[7];
										bsum += tlw * cfa[8] + trw * cfa[9] + blw * cfa[10] + brw * cfa[11];
									}
								}
								int const pixels_per_channel = m_state.output_width * m_state.output_height;
								out_data_ptr[m_state.out_data_y * m_state.output_width + out_x + 0 * pixels_per_channel] = static_cast<T_OUT>(rsum + m_kernel.offset);
								out_data_ptr[m_state.out_data_y * m_state.output_width + out_x + 1 * pixels_per_channel] = static_cast<T_OUT>(gsum + m_kernel.offset);
								out_data_ptr[m_state.out_data_y * m_state.output_width + out_x + 2 * pixels_per_channel] = static_cast<T_OUT>(bsum + m_kernel.offset);
							}
						}

						// increment buffer processing index by kernel stride along y axis
						m_state.buffer_processing_index = (m_state.buffer_processing_index + m_state.input_width * pixel_stride) % m_state.buffer_size;

						// new row in output image
						m_state.out_data_y++;

						// check if the output image is already finished
						if (m_state.out_data_y >= m_state.output_height * num_planes)
						{
							finished = true;
							break;
						}
					}

					// new row in input image
					m_state.input_row_counter++;

					// reset when next image plane is reached
					if (m_state.input_row_counter >= m_state.input_height)
					{
						++m_state.input_plane_counter;
						m_state.input_row_counter = 0;
						m_state.buffer_index = 0;
						m_state.buffer_row_start_index = 0;
						m_state.buffer_processing_index = 0;
						m_state.out_data_y = m_state.input_plane_counter * m_state.output_height;
					}
				}
			}

			if (m_output.negative)
			{
				*m_output.negative = negative;
			}

			if (finished)
			{
				return -1;
			}

			return 0;
		}

	private:
		DataKernel<T_IN, T_OUT> m_kernel;
		DataOutput<T_IN, T_OUT> m_output;
		DataIteratorState<T_IN, T_OUT> m_state;
	};

	template<typename T_IN, typename T_OUT>
	int ReadData(fitsfile* fptr, int datatype, int width, int height, Loader::DataKernel<T_IN, T_OUT>& kernel, Loader::DataOutput<T_IN, T_OUT>& output, int* status)
	{
		iteratorCol cols[1];

		fits_iter_set_file(&cols[0], fptr);
		fits_iter_set_iotype(&cols[0], InputCol);
		fits_iter_set_datatype(&cols[0], datatype);

		Loader::DataIterator<T_IN, T_OUT> iterator{ width, height, kernel, output };

		typedef int(*IteratorFn)(long totaln, long offset, long firstn, long nvalues, int narrays, iteratorCol* data, void* userPointer);
		IteratorFn fn = {
			[](long totaln, long offset, long firstn, long nvalues, int narrays, iteratorCol* data, void* userPointer)
			{
				return static_cast<Loader::DataIterator<T_IN, T_OUT>*>(userPointer)->Read(totaln, offset, firstn, nvalues, narrays, data);
			}
		};

		fits_iterate_data(1, cols, 0, 0, fn, &iterator, status);

		if (*status == -1)
		{
			*status = 0;
		}

		return *status;
	}
}
