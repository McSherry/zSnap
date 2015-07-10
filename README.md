# HTTP Uploader branch

This branch is used for development of the HTTP uploader plugin, which will be built into zSnap.
The plugin allows you to upload images over HTTP, using HTTP File upload (`multipart/form-data` upload).
Additionally, HTTP Basic Authentication can be used to submit credentials.

The resulting image URL will be grabbed from the response. If the response simply contains the URL, 
the plugin will pick it up automatically. When this is not the case, you can supply a regular expression
to filter the URL from the response. This way, a wide variety of servers and websites can be accommodated for.

If you need a more detailed implementation (for instance, parsing several variables from a response JSON string), 
it is recommended that you write your own plugin instead, and reference `zSnap.Uploaders.HttpPost`.
You will then have access to the static `HttpUploader.UploadFile()` methods, which contain the core functionality of this plugin.

There are two UploadFile methods available. The first overload looks like this:
````
HttpResponseMessage UploadFile(Image image, string filename, Uri remoteHost)
````
and can be used to upload files without authenticating. It is functionally identical to calling the second method and passing `null` to the `username` and `token` parameters.

The second method looks like this:
````
HttpResponseMessage UploadFile(Image image, string filename, Uri remoteHost, string username, string token)
````
This method will upload `image` to the server located at `remoteHost`. `filename` is passed to the server, and, depending on the implementation of the server software, may or may not be respected. `username` and `token` are the HTTP Basic Authentication parameters. Setting these to null will disable authentication.
