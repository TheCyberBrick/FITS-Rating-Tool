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

// https://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html

#pragma once

#include <ppl.h>

#include "fitsattributes.h"

namespace Processing
{

	struct ChannelStretchParameters
	{
		int max_input = 65535;

		float shadows = 0.0f;
		float highlights = 1.0f;
		float midtones = 0.5f;
	};

	struct ImageStretchParameters
	{
		ChannelStretchParameters rk{}, g{}, b{};

		inline ChannelStretchParameters const& operator[](int index) const { return const_cast<ImageStretchParameters*>(this)->GetChannelParameters(index); }
		inline ChannelStretchParameters& operator[](int index) { return GetChannelParameters(index); }

	private:
		inline ChannelStretchParameters& GetChannelParameters(int index)
		{
			switch (index)
			{
			default:
				return rk;
			case 1:
				return g;
			case 2:
				return b;
			}
		}
	};

	template<typename T>
	T Median(std::valarray<T>& values)
	{
		int const middle = static_cast<int>(values.size() / 2);
		std::nth_element(std::begin(values), std::begin(values) + middle, std::end(values));
		return values[middle];
	}

	template<typename T>
	int EstimateMaximum(std::valarray<T>& data, int offset, int width, int height, int max)
	{
		int range = 1;
		if (max > 255)
		{
			range = 65535;
		}
		else if (max > 1)
		{
			range = 255;
		}
		return range;
	}

	template<typename T>
	void ComputeChannelStretch(std::valarray<T>& data, int offset, int width, int height, ChannelStretchParameters* channel_params)
	{
		int const max_samples = 262144;
		int const slice_stride = std::max(1, width * height / max_samples);
		std::valarray<T> samples = data[std::slice(offset, width * height / slice_stride, slice_stride)];

		float M = Median(samples);

		T max = 0;
		for (int i = 0; i < samples.size(); ++i)
		{
			T const s = samples[i];
			max = std::max(max, s);
			samples[i] = s > M ? s - M : M - s;
		}

		channel_params->max_input = EstimateMaximum(data, offset, width, height, max);

		float const normalization_factor = 1.0f / static_cast<float>(channel_params->max_input);

		M *= normalization_factor;

		float const MADN = 1.4826f * Median(samples) * normalization_factor;

		float const B = 0.25f;
		float const C = -2.8f;

		bool const a = M > 0.5f;

		float const s = a || MADN == 0 ? 0.0f : std::min(1.0f, std::max(0.0f, M + C * MADN));
		float const h = !a || MADN == 0 ? 1.0f : std::min(1.0f, std::max(0.0f, M - C * MADN));

		auto midtonesTransferFunction = [](float const x, float const m)
		{
			if (x <= 0)
			{
				return 0.0f;
			}
			else if (x == m)
			{
				return 0.5f;
			}
			else if (x >= 1)
			{
				return 1.0f;
			}
			else
			{
				return ((m - 1.0f) * x) / ((2.0f * m - 1.0f) * x - m);
			}
		};

		float const m = !a ? midtonesTransferFunction(M - s, B) : midtonesTransferFunction(B, h - M);

		channel_params->highlights = h;
		channel_params->midtones = m;
		channel_params->shadows = s;
	}

	template<typename T>
	void ComputeImageStretch(std::valarray<T>& data, Loader::FITSImageDim const& size, ImageStretchParameters* image_params)
	{
		int const pixels_per_channel = size.nx * size.ny;
		concurrency::parallel_for(0, size.nc, [&](int c) { ComputeChannelStretch(data, pixels_per_channel * c, size.nx, size.ny, &(*image_params)[c]); });
	}

	template<typename T>
	void StretchChannel(std::valarray<T>& data, int offset, int width, int height, ChannelStretchParameters const& channel_params, uint32_t* histogram, size_t histogram_size)
	{
		int const max_output = 255;

		float const h = channel_params.highlights;
		float const m = channel_params.midtones;
		float const s = channel_params.shadows;

		float const h_scaled = h * channel_params.max_input;
		float const m_scaled = m * channel_params.max_input;
		float const s_scaled = s * channel_params.max_input;

		float const clip_denominator = h_scaled - s_scaled;
		float const clip_scale = std::abs(clip_denominator) > 0.0001f ? 1.0f / clip_denominator : 1.0f;

		float const mtf_numerator = (m_scaled - channel_params.max_input) * max_output;
		float const mtf_denominator = (2 * m_scaled - channel_params.max_input);

		float const a1 = mtf_numerator * clip_scale;
		float const a2 = s_scaled * a1;

		float const b1 = mtf_denominator * clip_scale;
		float const b2 = s_scaled * b1 + m_scaled;

		//Clip: x = (x - s_scaled) * clip_scale;
		//MTF:  x = (mtf_numerator * x) / (mtf_denominator * x - m_scaled);
		//-->   x = (x * a1 - a2) / (x * b1 - b2);

		int const start = offset;
		int const end = start + width * height;

		if (histogram == nullptr)
		{
			for (int i = start; i < end; ++i)
			{
				T const x = data[i];

				if (x < s_scaled)
				{
					data[i] = 0;
				}
				else if (x > h_scaled)
				{
					data[i] = max_output;
				}
				else
				{
					data[i] = static_cast<T>((x * a1 - a2) / (x * b1 - b2));
				}
			}
		}
		else
		{
			float const histogram_scale = 1.0f / max_output * (histogram_size - 1);

			for (int i = start; i < end; ++i)
			{
				T const x = data[i];

				if (x < s_scaled)
				{
					data[i] = 0;
					++histogram[0];
				}
				else if (x > h_scaled)
				{
					data[i] = max_output;
					++histogram[histogram_size - 1];
				}
				else
				{
					float const stretched = (x * a1 - a2) / (x * b1 - b2);
					data[i] = static_cast<T>(stretched);
					++histogram[std::max(std::min(static_cast<size_t>(stretched * histogram_scale), histogram_size - 1), size_t{ 0 })];
				}
			}
		}
	}

	template<typename T>
	void StretchImage(std::valarray<T>& data, Loader::FITSImageDim const& size, ImageStretchParameters const& image_params, uint32_t* histogram_rk, uint32_t* histogram_g, uint32_t* histogram_b, size_t histogram_size)
	{
		int const pixels_per_channel = size.nx * size.ny;
		uint32_t* const histograms[] = { histogram_rk, histogram_g, histogram_b };
		concurrency::parallel_for(0, size.nc, [&](int c) { StretchChannel(data, pixels_per_channel * c, size.nx, size.ny, image_params[c], histograms[c == 0 || c > 2 ? 0 : c], histogram_size); });
	}

}
