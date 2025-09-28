FiendFriend
=========

FiendFriend is a C# app to show images pinned to the desktop window, intended to show a 'virtual pet'.

## Configuration

FiendFriend reads configuration files from a json file, accepted as a command line parameter (defaulting to appsettings.json).

SpritePath - the path to the sprite images

ImageChangeIntervalMinutes - automatically change the image to a random sprite every x minutes (-1 to disable)

EnableDoubleClickToChangeImage - double click to change the image

FlipImagesHorizontally - flip the images horizontally


FiendFriend provides mechanisms so the image can be changed by other applications:

### NamedPipe
Enabled - if the named pipe communication channel is enabled
PipeName - the name of the named pipe
    
### WebServer
Enabled - if the webserver communication channel is enabled
Port - the port to listen on
Host - the hostname to listen on


### Example Configuration

```json
{
  "FiendFriend": {
    "SpritePath": "E:\\source\\extrpact\\src\\bin\\Debug\\net8.0\\4\\sprites\\Yue",
    "ImageChangeIntervalMinutes": -1,
    "EnableDoubleClickToChangeImage": true,
    "FlipImagesHorizontally": true
  },
  "Communication": {
    "NamedPipe": {
      "Enabled": false,
      "PipeName": "FiendFriend_IPC"
    },
    "WebServer": {
      "Enabled": true,
      "Port": 8080,
      "Host": "localhost"
    }
  }
}
```

The Examples folder in the repository contains sample HTML and PowerShell scripts to interact with FiendFriend via both of these communication channels.

## Download

Compiled downloads are not available.

## Compiling

To clone and run this application, you'll need [Git](https://git-scm.com) and [.NET](https://dotnet.microsoft.com/) installed on your computer. From your command line:

```
# Clone this repository
$ git clone https://github.com/btigi/fiendfriend

# Go into the repository
$ cd src

# Build  the app
$ dotnet build
```

## Licencing

FiendFriendr is licensed under the MIT license. Full licence details are available in license.md