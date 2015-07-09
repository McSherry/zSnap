# HTTP Uploader branch

This branch is used for development of the HTTP uploader plugin, which will be built into zSnap.
The plugin allows you to upload images over HTTP, using HTTP File upload (`multipart/form-data` upload).
Additionally, HTTP Basic Authentication can be used to submit credentials.

The resulting image URL grabbed from the response. If the response simply contains the URL, 
the plugin will pick it up automatically. When this is not the case, the user can supply a regular expression
to filter the URL from the response. This way, a wide variety of servers and websites can be accommodated for.

If you need a more detailed implementation (for instance, parsing several variables from a response JSON string), 
it is recommended that you write your own plugin instead, and reference `zSnap.Uploaders.HttpPost`.
You will then have access to the static UploadFile() methods, which contain the core functionality of this plugin.
