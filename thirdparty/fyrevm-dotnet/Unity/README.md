# FyreVM Unity Integration Example

## Overview

This project is a basic example of integrating the FyreVM library into a Unity3D project. It illustrates basic interaction between Unity objects and FyreVM. The project was built using Unity version 4.6.2. The project may or may not work on earlier versions.

## Inform 7

### Building

Install the latest version of Inform 7 (http://inform7.com/download/). 

Install the FyreVM Support extension found elsehwere in the FyreVM GitHub reporsitory.

    FyreVM Support - handles the Channel IO communication protocols implemented in FyreVM.

Install the included Inform extension located under "Inform\Extensions" in this repository. These extensions include:
	
    FyreVM Unity Support - handles the output of Unity-specific data into the apporpriate FyreVM channels.
	
To verify that the extensions are installed and working properly, open the 'FyreVM Unity Demo.inform' project in Inform and hit play. You should be able to navigate between the three rooms defined in the simple Inform script provided.

### Editing

To add extra functionality beyond the simple room navigation illustrated in this demo, you must add a new channel to the FyreVM Support extension and an output for that channel in the FyreVM Unity Support extension.

After adding any new functionality, make sure the settings are set to build a Glulx file and that "Bind up into a Blorb file on release" is not checked.

Select the Release option and it will generate a .ulx file for you.

Change the .ulx extension to .bytes and copy it over to the Unity folder: "\Unity\Assets\Resources\". This will allow unity to be able to read in the new Glulx file.

## Unity

### Building

Open up the Unity project located under "\Unity" in this repository using Unity 4.6.2 or later.

Press play in the editor and you should be able to navigate between the rooms defined in the Inform script.

### Editing

The scripts are located at "\Unity\Assets\Scripts\"

All of the glulx Channel IO processing is done via "GlulxStateService.cs"

To add new channel support, edit "eStateOutputChannels" in "GameConstants.cs" to include your new channels. Then add parsing logic to "GlulxStateService.ProcessGlulxOutput" to parse data from those new channels appropriately.
