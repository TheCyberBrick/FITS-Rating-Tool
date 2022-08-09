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

// PSF fitting adapted from PixInsight Class Library (https://gitlab.com/pixinsight/PCL).
// See /GuiApp/Resources/LICENSE_PCL.txt for the PCL license.

#include "photometry.h"

#include <numeric>

namespace Photometry
{

	void Extractor::Winsorize(std::vector<double>& values, double fraction)
	{
		size_t lo = (size_t)floor(fraction * values.size());
		size_t hi = values.size() - lo;
		double m = SortedMedian(values);
		for (size_t i = 0; i < lo; ++i)
		{
			values[i] = m;
		}
		for (size_t i = hi; i < values.size(); ++i)
		{
			values[i] = m;
		}
		/*for (int i = 0; i < lo; ++i)
		{
			values[i] = values[lo];
		}
		for (int i = hi; i < values.size(); ++i)
		{
			values[i] = values[hi - 1];
		}*/
	}

	void Extractor::FitError(double* crop, size_t w, size_t h, FitParameters& fit, double* star_residual, double* mean_signal)
	{
		MoffatParameters& moffat = fit.moffat;

		std::vector<double> adev(w * h);
		double* adev_ptr = adev.data();

		double x0 = (fit.w >> 1) + moffat.x0;
		double y0 = (fit.h >> 1) + moffat.y0;

		double s = std::sin(moffat.theta);
		double c = std::cos(moffat.theta);

		double sx = s / moffat.alpha_x;
		double cx = c / moffat.alpha_x;
		double sy = s / moffat.alpha_y;
		double cy = c / moffat.alpha_y;

		double A = cx * cx + sy * sy;
		double B = sx * sx + cy * cy;
		double C = 2 * s * c * (1.0 / (moffat.alpha_x * moffat.alpha_x) - 1.0 / (moffat.alpha_y * moffat.alpha_y));

		const double* data_ptr = fit.data_ptr;

		double flux = 0.0;
		double zsum = 0.0;

		for (int y = 0; y < fit.h; ++y)
		{
			double dy = y - y0;
			double bdy = B * dy * dy;
			double cdy = C * dy;

			for (int x = 0; x < fit.w; ++x)
			{
				double dx = x - x0;
				double data = *data_ptr++;
				double z = 1.0 / std::pow(1 + A * dx * dx + bdy + cdy * dx, 4);
				*adev_ptr++ = std::abs(data - moffat.S0 - moffat.S1 * z);
				if (data > moffat.S0)
				{
					flux += (data - moffat.S0) * z;
					zsum += z;
				}
			}
		}

		std::sort(adev.begin(), adev.end(), std::greater<double>());
		Winsorize(adev, 0.1);

		*star_residual = std::accumulate(adev.begin(), adev.end(), 0.0) / adev.size();
		*mean_signal = flux / zsum;
	}

	int Extractor::FitMoffat(void* p, int m, int n, const double* parameters, double* fvec, int iflag)
	{
		if (MoffatParameters::_S0(parameters) < 0 || MoffatParameters::_S1(parameters) < 0)
		{
			std::fill_n(fvec, m, std::numeric_limits<double>::max());
			return 0;
		}

		FitParameters* fit = reinterpret_cast<FitParameters*>(p);

		double x0 = (fit->w >> 1) + MoffatParameters::_x0(parameters);
		double y0 = (fit->h >> 1) + MoffatParameters::_y0(parameters);

		double s = std::sin(MoffatParameters::_theta(parameters));
		double c = std::cos(MoffatParameters::_theta(parameters));

		double sx = s / MoffatParameters::_alpha_x(parameters);
		double cx = c / MoffatParameters::_alpha_x(parameters);
		double sy = s / MoffatParameters::_alpha_y(parameters);
		double cy = c / MoffatParameters::_alpha_y(parameters);

		double A = cx * cx + sy * sy;
		double B = sx * sx + cy * cy;
		double C = 2 * s * c * (1.0 / (MoffatParameters::_alpha_x(parameters) * MoffatParameters::_alpha_x(parameters)) - 1.0 / (MoffatParameters::_alpha_y(parameters) * MoffatParameters::_alpha_y(parameters)));

		const double* data_ptr = fit->data_ptr;

		for (int y = 0; y < fit->h; ++y)
		{
			double dy = y - y0;
			double bdy = B * dy * dy;
			double cdy = C * dy;

			for (int x = 0; x < fit->w; ++x)
			{
				double dx = x - x0;
				*fvec++ = std::abs(*data_ptr++ - MoffatParameters::_S0(parameters) - MoffatParameters::_S1(parameters) / std::pow(1.0 + A * dx * dx + bdy + cdy * dx, 4));
			}
		}

		return 0;
	}

	template<typename T>
	double Extractor::SortedMedian(std::vector<T>& sorted_values)
	{
		size_t count = sorted_values.size();
		if (count <= 1)
		{
			return sorted_values[0];
		}
		else if (count % 2 == 0)
		{
			size_t half = count / 2 - 1;
			return (sorted_values[half] + sorted_values[half + 1]) * 0.5;
		}
		else
		{
			return sorted_values[(count - 1) / 2];
		}
	}

	template<typename T>
	double Extractor::Median(std::vector<T>& values)
	{
		const int middle = (int)(values.size() / 2);
		std::nth_element(std::begin(values), std::begin(values) + middle, std::end(values));
		return values[middle];
	}

	template<typename T>
	double Extractor::Median(typename std::vector<T>::iterator begin, typename std::vector<T>::iterator end)
	{
		const size_t count = end - begin;
		const int middle = (int)(count / 2);
		std::nth_element(begin, begin + middle, end);
		return begin[middle];
	}

	bool Extractor::FitPSF(std::valarray<float>& image, int image_width, double x, double y, double xmin, double ymin, double xmax, double ymax, PSF* psf, int* status)
	{
		size_t x1 = static_cast<size_t>(floor(xmin));
		size_t y1 = static_cast<size_t>(floor(ymin));
		size_t x2 = static_cast<size_t>(floor(xmax)) + 1;
		size_t y2 = static_cast<size_t>(floor(ymax)) + 1;

		size_t w = x2 - x1;
		size_t h = y2 - y1;

		std::valarray<double> crop(w * h);

		{
			double* crop_ptr = &crop[0];
			for (size_t y = y1; y < y2; ++y)
			{
				for (size_t x = x1; x < x2; ++x)
				{
					*crop_ptr++ = image[x + y * image_width];
				}
			}
		}

		double background = 0;

		{
			std::vector<double> edge(std::max(w, h));

			for (int i = 0; i < 4; ++i)
			{
				size_t x, y, stride, count;
				switch (i)
				{
				case 0:
					x = 0;
					y = 0;
					stride = 1;
					count = w;
					break;
				case 1:
					x = w - 1;
					y = 0;
					stride = w;
					count = h;
					break;
				case 2:
					x = 0;
					y = h - 1;
					stride = 1;
					count = w;
					break;
				case 3:
					x = 0;
					y = 0;
					stride = w;
					count = h;
					break;
				}

				{
					double* crop_ptr = &crop[0];
					for (int offset = 0; offset < count; ++offset)
					{
						edge[offset] = *crop_ptr;
						crop_ptr += stride;
					}
				}

				background += Median<double>(std::begin(edge), std::begin(edge) + count);
			}
		}

		background *= 0.25f;

		FitParameters parameters;
		parameters.data_ptr = &crop[0];
		parameters.w = static_cast<int>(w);
		parameters.h = static_cast<int>(h);
		parameters.moffat.S0 = background;
		parameters.moffat.S1 = crop.max() - background;
		parameters.moffat.x0 = x - (x1 + x2) * 0.5;
		parameters.moffat.y0 = y - (y1 + y2) * 0.5;
		parameters.moffat.alpha_x = parameters.moffat.alpha_y = 0.15 * 0.5 * ((xmax - xmin) + (ymax - ymin));
		parameters.moffat.theta = 0;
		parameters.moffat.beta = 4;

		int m = static_cast<int>(crop.size());
		int n = 8;

		std::valarray<double> fvec(m);
		std::valarray<int> iwa(n);
		std::valarray<double> wa(m * n + 5 * n + m);

		*status = lmdif1(Extractor::FitMoffat, &parameters, m, n, parameters.moffat.parameters, &fvec[0], 1.0e-08, &iwa[0], &wa[0], static_cast<int>(wa.size()));
		if (*status != 1 && *status != 2 && *status != 3)
		{
			return false;
		}

		psf->x = x1 + (w >> 1) + parameters.moffat.x0;
		psf->y = y1 + (h >> 1) + parameters.moffat.y0;

		parameters.moffat.alpha_x = std::abs(parameters.moffat.alpha_x);
		parameters.moffat.alpha_y = std::abs(parameters.moffat.alpha_y);

		if (parameters.moffat.alpha_x < parameters.moffat.alpha_y)
		{
			std::swap(parameters.moffat.alpha_x, parameters.moffat.alpha_y);
		}

		if (MoffatParameters::FWHM(parameters.moffat.alpha_x) > std::min(x2 - x1, y2 - y1))
		{
			// FWHM larger than crop
			return false;
		}

		psf->alpha_x = parameters.moffat.alpha_x;
		psf->alpha_y = parameters.moffat.alpha_y;

		psf->eccentricity = std::sqrt(1 - psf->alpha_y * psf->alpha_y / (psf->alpha_x * psf->alpha_x));

		double mean_signal;

		if (std::abs(psf->alpha_x - psf->alpha_y) < 0.01)
		{
			FitError(&crop[0], w, h, parameters, &psf->residual, &mean_signal);
			psf->theta = parameters.moffat.theta = 0.0;
		}
		else
		{
			double theta = parameters.moffat.theta;

			double residual, best_theta;
			for (int i = 0; i < 4; i++)
			{
				parameters.moffat.theta = theta + i * 1.570795;
				double tmp_mean_signal;
				FitError(&crop[0], w, h, parameters, &residual, &tmp_mean_signal);
				if (i == 0 || residual < psf->residual)
				{
					psf->residual = residual;
					mean_signal = tmp_mean_signal;
					best_theta = parameters.moffat.theta;
				}
			}

			psf->theta = parameters.moffat.theta = std::atan2(std::sin(best_theta), std::cos(best_theta));
		}

		psf->residual /= mean_signal;

		return true;
	}

	bool Extractor::Extract(Loader::FITSInfo& fit, std::valarray<float>& image, Catalog** catalog_out, int* status, Callback callback)
	{
		if (callback != nullptr && !callback(Phase::Median, 0, 0, 0))
		{
			return false;
		}

		float* data = &image[0];

		int n = (fit.attributes().data.out_dim.nx * fit.attributes().data.out_dim.ny);

		std::vector<float> work_image(data, data + n);

		// Get median as first background estimate
		double median = Median(work_image);

		*catalog_out = new Catalog();
		Catalog* catalog = *catalog_out;

		catalog->statistics.median = median;
		for (int i = 0; i < work_image.size(); ++i)
		{
			catalog->statistics.median_mad += std::abs(median - work_image[i]);
		}
		catalog->statistics.median_mad /= work_image.size();

		work_image.assign(data, data + n);

		float* work_image_ptr = &work_image[0];

		if (callback != nullptr && !callback(Phase::Background, 0, 0, 0))
		{
			return false;
		}

		// Set up SEP image
		sep_image simage{};
		simage.data = work_image_ptr;
		simage.noise = nullptr;
		simage.mask = nullptr;
		simage.segmap = nullptr;
		simage.dtype = SEP_TFLOAT;
		simage.ndtype = 0;
		simage.sdtype = 0;
		simage.w = fit.attributes().data.out_dim.nx;
		simage.h = fit.attributes().data.out_dim.ny;
		simage.noiseval = 0.0;
		simage.noise_type = SEP_NOISE_NONE;
		simage.gain = 0.0;
		simage.maskthresh = 0.0;

		// Create background model
		if (*status = sep_background(&simage, m_parameters.background_tile_size, m_parameters.background_tile_size, m_parameters.background_filter_size, m_parameters.background_filter_size, median, &catalog->sep_background))
		{
			if (catalog->sep_background != nullptr)
			{
				sep_bkg_free(catalog->sep_background);
			}
			return false;
		}

		// Subtract background model from work image
		if (*status = sep_bkg_subarray(catalog->sep_background, work_image_ptr, SEP_TFLOAT))
		{
			if (catalog->sep_background != nullptr)
			{
				sep_bkg_free(catalog->sep_background);
			}
			return false;
		}

		// Set image noise to the stddev of the background model
		simage.noise_type = SEP_NOISE_STDDEV;
		simage.noiseval = sep_bkg_globalrms(catalog->sep_background);

		// Iteratively estimate noise from background subtracted image
		std::vector<float> noise_work_image(work_image_ptr, work_image_ptr + n);
		double sigma = simage.noiseval; // Use SEP's noise value as initial estimate
		int noise_pixels = n;
		for (int i = 0; i < m_parameters.noise_i; ++i)
		{
			double ksigma = m_parameters.noise_k * sigma;
			int count = 0;
			double prevSigma = sigma;
			sigma = 0.0;
			for (int j = 0; j < noise_pixels; ++j)
			{
				float value = noise_work_image[j];
				if (std::abs(value) < ksigma)
				{
					noise_work_image[count++] = value;
					sigma += value * value;
				}
			}
			noise_pixels = count;
			sigma = std::sqrt(sigma / (double)noise_pixels);
			if (i >= 1 && std::abs(prevSigma - sigma) / prevSigma < m_parameters.noise_eps)
			{
				break;
			}
		}
		catalog->statistics.noise = sigma;
		catalog->statistics.noise_ratio = (double)noise_pixels / (double)n;

		if (callback != nullptr && !callback(Phase::Extract, 0, 0, 0))
		{
			return false;
		}

		float default_filter[9] = { 1.0, 2.0, 1.0, 2.0, 4.0, 2.0, 1.0, 2.0, 1.0 };

		float* filter;
		int filter_width, filter_height;
		if (m_parameters.extract_filter != nullptr)
		{
			filter = m_parameters.extract_filter;
			filter_width = m_parameters.extract_filter_width;
			filter_height = m_parameters.extract_filter_height;
		}
		else
		{
			filter = default_filter;
			filter_width = 3;
			filter_height = 3;
		}

		// Extract objects from image
		if (*status = sep_extract(&simage, m_parameters.extract_threshold, SEP_THRESH_REL, m_parameters.extract_min_obj_pixels, filter, filter_width, filter_height, SEP_FILTER_MATCHED, m_parameters.extract_deblend_nthresh, m_parameters.extract_deblend_contrast, 1, m_parameters.extract_cleaning_aggressiveness, &catalog->sep_catalog))
		{
			sep_bkg_free(catalog->sep_background);
			if (catalog->sep_catalog != nullptr)
			{
				sep_catalog_free(catalog->sep_catalog);
			}
			return false;
		}

		std::vector<double> eccentricities;
		catalog->statistics.eccentricity_max = 0;
		catalog->statistics.eccentricity_min = std::numeric_limits<double>::max();
		catalog->statistics.eccentricity_mean = 0;
		catalog->statistics.eccentricity_median = 0;

		std::vector<double> snrs;
		catalog->statistics.snr_max = 0;
		catalog->statistics.snr_min = std::numeric_limits<double>::max();
		catalog->statistics.snr_mean = 0;
		catalog->statistics.snr_median = 0;

		std::vector<double> fwhms;
		catalog->statistics.fwhm_max = 0;
		catalog->statistics.fwhm_min = std::numeric_limits<double>::max();
		catalog->statistics.fwhm_mean = 0;
		catalog->statistics.fwhm_median = 0;

		std::vector<double> hfrs;
		catalog->statistics.hfr_max = 0;
		catalog->statistics.hfr_min = std::numeric_limits<double>::max();
		catalog->statistics.hfr_mean = 0;
		catalog->statistics.hfr_median = 0;

		std::vector<double> residuals;
		catalog->statistics.residual_max = 0;
		catalog->statistics.residual_min = std::numeric_limits<double>::max();
		catalog->statistics.residual_mean = 0;
		catalog->statistics.residual_median = 0;

		int nobj = catalog->sep_catalog->nobj;

		for (int i = 0; i < nobj; i++)
		{
			if (callback != nullptr && !callback(Phase::Object, nobj, i, static_cast<int>(catalog->objects.size())))
			{
				return false;
			}

			double a = catalog->sep_catalog->a[i];
			double b = catalog->sep_catalog->b[i];
			double rab = sqrt(a * a + b * b);

			if (rab > 0.8 && rab < 10)
			{
				Object obj;
				obj.catalog_index = i;

				double x = catalog->sep_catalog->x[i];
				double y = catalog->sep_catalog->y[i];

				double x_min = catalog->sep_catalog->xmin[i];
				double y_min = catalog->sep_catalog->ymin[i];
				double x_max = catalog->sep_catalog->xmax[i];
				double y_max = catalog->sep_catalog->ymax[i];

				obj.x_min = x_min;
				obj.y_min = y_min;
				obj.x_max = x_max;
				obj.y_max = y_max;

				double theta = catalog->sep_catalog->theta[i];

				double cxx = catalog->sep_catalog->cxx[i];
				double cyy = catalog->sep_catalog->cyy[i];
				double cxy = catalog->sep_catalog->cxy[i];

				// Calculate kron radius to figure out how large the aperture must be to include most of the flux
				if (*status = sep_kron_radius(&simage, x, y, cxx, cyy, cxy, 6.0, 0, &obj.kron_radius, &obj.kron_flag))
				{
					continue;
				}

				double area;

				// Calculate flux
				if (*status = sep_sum_ellipse(&simage, x, y, a, b, theta, m_parameters.photometry_kron_radius_multiple * obj.kron_radius, 0, 1, 0, &obj.ellipse_sum, &obj.ellipse_sum_err, &area, &obj.ellipse_sum_flag))
				{
					continue;
				}

				double geometric_mean = sqrt(a * b); // radius of circle with area equal to that of the ellipse
				if (obj.kron_radius * geometric_mean < 1.75)
				{
					sep_sum_circle(&simage, x, y, 1.75, 0, 1, 0, &obj.circle_sum, &obj.circle_sum_err, &area, &obj.circle_sum_flag);
					obj.flux = obj.circle_sum;
					obj.flux_err = obj.circle_sum_err;
					obj.flux_flag = obj.circle_sum_flag;
				}
				else
				{
					obj.circle_sum = obj.circle_sum_err = obj.circle_sum_flag = 0;
					obj.flux = obj.ellipse_sum;
					obj.flux_err = obj.ellipse_sum_err;
					obj.flux_flag = obj.ellipse_sum_flag;
				}

				// Calculate HFR
				double const flux_fraction = 0.5;
				if (*status = sep_flux_radius(&simage, x, y, 6.0 * a, 0, 5, 0, &obj.flux, &flux_fraction, 1, &obj.hfr, &obj.hfr_flag))
				{
					continue;
				}

				// Calculate SNR by treating the flux as signal and aperture area * background stddev as noise
				obj.snr = obj.flux / sqrt(obj.flux + area * area * 3.14159 * simage.noiseval /*stddev*/);
				// TODO Needs to consider e-/ADU gain
				// TODO SNR fails when image is float and not counts
				if (obj.snr < m_parameters.photometry_min_snr || obj.hfr > m_parameters.photometry_max_hfr)
				{
					continue;
				}

				if (m_parameters.psf_fit)
				{
					// Fit Moffat PSF
					if (!FitPSF(image, fit.attributes().data.out_dim.nx, catalog->sep_catalog->x[i], catalog->sep_catalog->y[i], x_min, y_min, x_max, y_max, &obj.psf, status))
					{
						continue;
					}
				}
				else
				{
					// Approximate PSF from SEP moments
					obj.psf.x = x;
					obj.psf.y = y;
					obj.psf.alpha_x = obj.psf.alpha_y = obj.psf.theta = obj.psf.fwhm_x = obj.psf.fwhm_y = obj.psf.residual = 0;
					obj.psf.fwhm = 2.0 * sqrt(log(2) * (a * a + b * b));
					obj.psf.eccentricity = std::sqrt(1 - b * b / (a * a));
				}

				catalog->statistics.residual_min = std::min(catalog->statistics.residual_min, obj.psf.residual);

				catalog->objects.emplace_back(obj);
			}
		}

		if (callback != nullptr && !callback(Phase::Statistics, nobj, 0, static_cast<int>(catalog->objects.size())))
		{
			return false;
		}

		double weight_sum = 0.0;

		// Calculate image statistics
		for (int i = 0; i < catalog->objects.size(); ++i)
		{
			Object& p = catalog->objects[i];
			PSF& psf = p.psf;

			psf.fwhm_x = MoffatParameters::FWHM(psf.alpha_x);
			psf.fwhm_y = MoffatParameters::FWHM(psf.alpha_y);
			psf.fwhm = MoffatParameters::FWHM(sqrt(psf.alpha_x * psf.alpha_y));

			psf.weight = m_parameters.psf_fit ? catalog->statistics.residual_min / psf.residual : 1.0;
			weight_sum += psf.weight;

			fwhms.push_back(psf.fwhm);
			catalog->statistics.fwhm_max = std::max(catalog->statistics.fwhm_max, psf.fwhm);
			catalog->statistics.fwhm_min = std::min(catalog->statistics.fwhm_min, psf.fwhm);
			catalog->statistics.fwhm_mean += psf.fwhm * psf.weight;

			eccentricities.push_back(psf.eccentricity);
			catalog->statistics.eccentricity_max = std::max(catalog->statistics.eccentricity_max, psf.eccentricity);
			catalog->statistics.eccentricity_min = std::min(catalog->statistics.eccentricity_min, psf.eccentricity);
			catalog->statistics.eccentricity_mean += psf.eccentricity * psf.weight;

			snrs.push_back(p.snr);
			catalog->statistics.snr_max = std::max(catalog->statistics.snr_max, p.snr);
			catalog->statistics.snr_min = std::min(catalog->statistics.snr_min, p.snr);
			catalog->statistics.snr_mean += p.snr * psf.weight;

			hfrs.push_back(p.hfr);
			catalog->statistics.hfr_max = std::max(catalog->statistics.hfr_max, p.hfr);
			catalog->statistics.hfr_min = std::min(catalog->statistics.hfr_min, p.hfr);
			catalog->statistics.hfr_mean += p.hfr * psf.weight;

			residuals.push_back(psf.residual);
			catalog->statistics.residual_max = std::max(catalog->statistics.residual_max, psf.residual);
			catalog->statistics.residual_mean += psf.residual * psf.weight;
		}

		if (fwhms.size() > 0)
		{
			catalog->statistics.fwhm_median = Median(fwhms);
		}
		if (eccentricities.size() > 0)
		{
			catalog->statistics.eccentricity_median = Median(eccentricities);
		}
		if (snrs.size() > 0)
		{
			catalog->statistics.snr_median = Median(snrs);
		}
		if (hfrs.size() > 0)
		{
			catalog->statistics.hfr_median = Median(hfrs);
		}
		if (residuals.size() > 0)
		{
			catalog->statistics.residual_median = Median(residuals);
		}

		for (int i = 0; i < catalog->objects.size(); ++i)
		{
			catalog->statistics.fwhm_mad += std::abs(catalog->statistics.fwhm_median - fwhms[i]);
			catalog->statistics.eccentricity_mad += std::abs(catalog->statistics.eccentricity_median - eccentricities[i]);
			catalog->statistics.snr_mad += std::abs(catalog->statistics.snr_median - snrs[i]);
			catalog->statistics.hfr_mad += std::abs(catalog->statistics.hfr_median - hfrs[i]);
			catalog->statistics.residual_mad += std::abs(catalog->statistics.residual_median - residuals[i]);
		}

		if (weight_sum > 0)
		{
			catalog->statistics.eccentricity_mean /= weight_sum;
			catalog->statistics.snr_mean /= weight_sum;
			catalog->statistics.fwhm_mean /= weight_sum;
			catalog->statistics.hfr_mean /= weight_sum;
			catalog->statistics.residual_mean /= weight_sum;
		}
		else
		{
			catalog->statistics.eccentricity_mean = 0;
			catalog->statistics.snr_mean = 0;
			catalog->statistics.fwhm_mean = 0;
			catalog->statistics.hfr_mean = 0;
			catalog->statistics.residual_mean = 0;
		}

		if (catalog->objects.size() > 0)
		{
			catalog->statistics.fwhm_mad /= catalog->objects.size();
			catalog->statistics.eccentricity_mad /= catalog->objects.size();
			catalog->statistics.snr_mad /= catalog->objects.size();
			catalog->statistics.hfr_mad /= catalog->objects.size();
			catalog->statistics.residual_mad /= catalog->objects.size();
		}
		else
		{
			catalog->statistics.eccentricity_min = 0;
			catalog->statistics.snr_min = 0;
			catalog->statistics.fwhm_min = 0;
			catalog->statistics.hfr_min = 0;
			catalog->statistics.residual_min = 0;

			catalog->statistics.fwhm_mad = 0;
			catalog->statistics.eccentricity_mad = 0;
			catalog->statistics.snr_mad = 0;
			catalog->statistics.hfr_mad = 0;
			catalog->statistics.residual_mad = 0;
		}

		return true;
	}

}