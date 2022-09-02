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

#include <sep.h>
#include <cminpack.h>
#include <vector>

#include "fitsloader.h"

namespace Photometry
{

	struct Parameters
	{
		// Size of background model tiles
		int background_tile_size = 64;
		// Filter size for background model calculation
		int background_filter_size = 3;

		// K parameter for K * Sigma noise ratio estimate
		double noise_k = 3;
		// Number of iterations for K * Sigma noise ratio estimate
		int noise_i = 16;
		// Convergence epsilon for K * Sigma noise ratio estimate
		double noise_eps = 0.01;

		// Extraction filter which objects must roughly match
		float* extract_filter = nullptr;
		// Width of extraction filter
		int extract_filter_width = 0;
		// Height of extraction filter
		int extract_filter_height = 0;
		// Object extraction threshold in background standard deviations
		float extract_threshold = 2.5f;
		// Minimum number of pixels an object must have
		int extract_min_obj_pixels = 21;
		// Number of deblending thresholds
		int extract_deblend_nthresh = 32;
		// Minimum contrast ratio used for object deblending
		double extract_deblend_contrast = 0.005;
		// Aggressiveness of cleaning process
		double extract_cleaning_aggressiveness = 1.0;

		// Radius of flux aperture is kron_radius_multiple * kron_radius
		double photometry_kron_radius_multiple = 2.5;
		// Minimum SNR for an object to be accepted
		double photometry_min_snr = 10;
		// Maximum HFD for an object to be accepted
		double photometry_max_hfr = 15;

		// Whether the PSF should be fitted
		bool psf_fit = true;
	};

	struct PSF
	{
		double x = 0;
		double y = 0;
		double alpha_x = 0;
		double alpha_y = 0;
		double theta = 0;
		double residual = 0;
		double weight = 0;
		double fwhm_x = 0;
		double fwhm_y = 0;
		double fwhm = 0;
		double eccentricity = 0;
	};

	struct MoffatParameters
	{
		double parameters[8]{};
		double& S0, & S1, & x0, & y0, & alpha_x, & alpha_y, & theta, & beta;

		MoffatParameters() : parameters(),
			S0(parameters[0]), S1(parameters[1]),
			x0(parameters[2]), y0(parameters[3]),
			alpha_x(parameters[4]), alpha_y(parameters[5]),
			theta(parameters[6]), beta(parameters[7]) {}

		static inline double _S0(const double* parameters) { return parameters[0]; }
		static inline double _S1(const double* parameters) { return parameters[1]; }
		static inline double _x0(const double* parameters) { return parameters[2]; }
		static inline double _y0(const double* parameters) { return parameters[3]; }
		static inline double _alpha_x(const double* parameters) { return parameters[4]; }
		static inline double _alpha_y(const double* parameters) { return parameters[5]; }
		static inline double _theta(const double* parameters) { return parameters[6]; }
		static inline double _beta(const double* parameters) { return parameters[7]; }

		// http://www.aspylib.com/doc/aspylib_fitting.html#circular-moffat-psf
		static inline double FWHM(const double alpha) { return 0.869958884 * alpha; }
	};

	struct FitParameters
	{
		double* data_ptr = nullptr;
		int w = 0, h = 0;
		MoffatParameters moffat{};
	};

	struct Object
	{
		int catalog_index = 0;

		double x_min = 0;
		double x_max = 0;
		double y_min = 0;
		double y_max = 0;

		double kron_radius = 0;
		short kron_flag = 0;

		double ellipse_sum = 0;
		double ellipse_sum_err = 0;
		short ellipse_sum_flag = 0;

		double circle_sum = 0;
		double circle_sum_err = 0;
		short circle_sum_flag = 0;

		double flux = 0;
		double flux_err = 0;
		short flux_flag = 0;

		double hfr = 0;
		short hfr_flag = 0;

		double snr = 0;

		PSF psf{};
	};

	struct Statistics
	{
		double median = 0;
		double median_mad = 0;

		double noise = 0;
		double noise_ratio = 0;

		double eccentricity_max = 0;
		double eccentricity_min = 0;
		double eccentricity_mean = 0;
		double eccentricity_median = 0;
		double eccentricity_mad = 0;

		double snr_max = 0;
		double snr_min = 0;
		double snr_mean = 0;
		double snr_median = 0;
		double snr_mad = 0;

		double fwhm_max = 0;
		double fwhm_min = 0;
		double fwhm_mean = 0;
		double fwhm_median = 0;
		double fwhm_mad = 0;

		double hfr_max = 0;
		double hfr_min = 0;
		double hfr_mean = 0;
		double hfr_median = 0;
		double hfr_mad = 0;

		double residual_max = 0;
		double residual_min = 0;
		double residual_mean = 0;
		double residual_median = 0;
		double residual_mad = 0;
	};

	struct Catalog
	{
		std::vector<Object> objects{};
		Statistics statistics{};

		sep_bkg* sep_background = nullptr;
		sep_catalog* sep_catalog = nullptr;

		~Catalog()
		{
			if (sep_background)
			{
				sep_bkg_free(sep_background);
				sep_background = nullptr;
			}
			if (sep_catalog)
			{
				sep_catalog_free(sep_catalog);
				sep_catalog = nullptr;
			}
		}
	};

	enum class Phase
	{
		Median, Background, Extract, Object, Statistics
	};

	typedef bool (*Callback)(Phase phase, int nobj, int iobj, int nstars);

	class Extractor
	{
	public:
		Extractor(Parameters& parameters) : m_parameters(parameters)
		{
		}

		bool Extract(Loader::FITSInfo& fit, std::valarray<float>& image, Catalog** catalog, int* status, Callback callback);

	private:
		Parameters m_parameters;

		template<typename T>
		double SortedMedian(std::vector<T>& sorted_values);

		template<typename T>
		double Median(std::vector<T>& values);

		template<typename T>
		double Median(typename std::vector<T>::iterator begin, typename std::vector<T>::iterator end);

		void Winsorize(std::vector<double>& sorted_values, double fraction);

		bool FitPSF(std::valarray<float>& image, int image_width, double x, double y, double xmin, double ymin, double xmax, double ymax, PSF* psf, int* status);

		void FitError(double* crop, size_t w, size_t h, FitParameters& fit, double* star_residual, double* mean_signal);

		static int FitMoffat(void* p, int m, int n, const double* a, double* fvec, int iflag);
	};

}