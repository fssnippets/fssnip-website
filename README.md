F# Snippets - Web site
======================

This is a new version of the [www.fssnip.net](http://www.fssnip.net) web site. The project 
implements most of the old functionality, but there is still a lot of room for improvements
and it is also a great way to get started with F# and Suave!

 * There is a [list of issues on GitHub](https://github.com/fssnippets/fssnip-website/issues) with
   various work items. Many of them not too hard and so this might be a fun way to contribute to
   your first open-source F# project! 

 * To discuss the Project, register at [Slack](http://foundation.fsharp.org/join) and visit the #gernal channel, for more infos see the [Slack Guide](http://fsharp.org/guides/slack/). 
* Feel free to ping me on Twitter at [@tomaspetricek](https://twitter.com/tomaspetricek).

Running web site locally
------------------------

There is one manual step you need to do before you can run the web site locally, which is to
download sample data. To do this, download [the data dump](https://github.com/fssnippets/fssnip-data/archive/master.zip)
from the `fssnip-data` project and extract the contents into `data` (so that you have `data/index.json`) in your root.

Once you're done with this, you can run `build.sh` (on Mac/Linux) or `build.cmd` (on Windows) to
run the web site. There is also a Visual Studio solution which can be started with <kbd>F5</kbd>,
but the build scripts are nicer because they automatically watch for changes.

Project architecture & structure
--------------------------------

In the current (development) version, the project uses file system as a data storage. In the
final version, we'll store the snippets in Azure blob storage (see the [issue for adding
this](https://github.com/tpetricek/FsSnip.Website/issues/6)).

The web page is mostly read-only. There are about 2 new snippets per day, so insertion can be
more expensive and not particularly sophisticated. Also, the metadata about all the snippets
is quite small (about 1MB JSON) and so we can keep all metadata in memory for browsing. When
a snippet is inserted, we update the in-memory metadata and save it to a JSON file (in a blob).

So, if you download the `data.zip` file (above), you get the following:

 - `data/index.json` - this is the JSON with metadata about all snippets. This is loaded when the
   web site starts (and it is updated & saved when a new snippet is inserted)
 - `data/formatted/<id>/<version>` is a file that contains formatted HTML for a snippet with
   a specified ID; we also support multiple versions of snippets.
 - `data/source/<id>/<version>` is a file with the original source code for a snippet

Other most important files and folders in the project are:

 - `app.fsx` defines the routing for web requests and puts everything together
 - `code/pages/*.fs` are files that handle specific things for individual pages
 - `code/common/*.fs` are common utilities, data access code etc.
 - `templates/*.html` are DotLiquid templates for various pages
 - `web/*` is folder with static files (CSS, JavaScript, images, etc.)
