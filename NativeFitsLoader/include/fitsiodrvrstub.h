#ifndef _FITSIODRVRSTUB_H
#define _FITSIODRVRSTUB_H

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
	void *instance;

	//Reading
	int(*open)(void *instance, char *filename, int rwmode, int *driverhandle);
	int(*read)(void *instance, int drivehandle, void *buffer, long nbytes);
	int(*size)(void *instance, int handle, LONGLONG *filesize);
	int(*seek)(void *instance, int handle, LONGLONG offset);

	//Writing
	int(*create)(void *instance, char *filename, int *handle);
	int(*write)(void *instance, int hdl, void *buffer, long nbytes);
	int(*flush)(void *instance, int handle);

	//I/O
	int(*close)(void *instance, int drivehandle);
} stubdriverimpl;

void CFITS_API fits_set_stub_driver_impl(
	void *instance,
	int(*open)(void *instance, char *filename, int rwmode, int *driverhandle),
	int(*read)(void *instance, int drivehandle, void *buffer, long nbytes),
	int(*size)(void *instance, int handle, LONGLONG *filesize),
	int(*seek)(void *instance, int handle, LONGLONG offset),
	int(*create)(void *instance, char *filename, int *handle),
	int(*write)(void *instance, int hdl, void *buffer, long nbytes),
	int(*flush)(void *instance, int handle),
	int(*close)(void *instance, int drivehandle)
);

void CFITS_API fits_set_stub_driver_impl_struct(stubdriverimpl impl);

stubdriverimpl CFITS_API fits_get_stub_driver_impl();

#ifdef __cplusplus
}
#endif

#endif
