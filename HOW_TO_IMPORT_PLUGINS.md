# How to Import and Use FFXIV Dalamud Plugins

This guide will walk you through the process of loading the Chocobo Colourized plugin (or any custom Dalamud plugin) into FFXIV.

---

## Prerequisites

Before you can load custom plugins, ensure you have:

1. **FINAL FANTASY XIV** installed and running
2. **XIVLauncher** installed (replaces the standard FFXIV launcher)
3. **Dalamud** enabled (comes with XIVLauncher)
4. The game has been launched **at least once** with Dalamud enabled

---

## Step 1: Locate Your Plugin DLL File

After building the Chocobo Colourized plugin, you need to find the compiled DLL file.

**Default Location:**
```
ChocoboColourized/bin/x64/Debug/ChocoboColourized.dll
```

Or if you built in Release mode:
```
ChocoboColourized/bin/x64/Release/ChocoboColourized.dll
```

**Important:** Note the **full absolute path** to this DLL file. You'll need it in the next steps.

Example path:
```
D:\temp\ChocoboColourized\bin\x64\Debug\ChocoboColourized.dll
```

---

## Step 2: Launch FFXIV with XIVLauncher

1. Open **XIVLauncher** (not the standard FFXIV launcher)
2. Log in with your credentials
3. Click **Play** to launch the game
4. Wait for the game to fully load to the character selection or main menu

---

## Step 3: Open Dalamud Settings

Once in-game, you need to access the Dalamud settings panel.

**Method 1: Chat Command**
- Press **Enter** to open the chat box
- Type: `/xlsettings`
- Press **Enter**

**Method 2: Dalamud Console**
- Open the Dalamud console (default: `Ctrl + Shift + D`)
- Type: `xlsettings`
- Press **Enter**

The Dalamud Settings window should now appear.

---

## Step 4: Add Plugin to Dev Plugin Locations

1. In the Dalamud Settings window, navigate to the **Experimental** tab
2. Look for the section labeled **"Dev Plugin Locations"**
3. Click the **"+"** button to add a new location
4. **Paste or type the full path** to your `ChocoboColourized.dll` file
   - Example: `D:\temp\ChocoboColourized\bin\x64\Debug\ChocoboColourized.dll`
5. Click **Save** or **Save and Close**

**Important Notes:**
- You only need to do this **once** per plugin
- The path is saved and will persist across game restarts
- Make sure the path points directly to the `.dll` file, not just the folder

---

## Step 5: Open the Plugin Installer

Now you need to enable the plugin.

**Method 1: Chat Command**
- Press **Enter** to open chat
- Type: `/xlplugins`
- Press **Enter**

**Method 2: Dalamud Console**
- Open the Dalamud console
- Type: `xlplugins`
- Press **Enter**

The Plugin Installer window will open.

---

## Step 6: Enable Your Plugin

1. In the Plugin Installer window, click on the **"Dev Tools"** tab at the top
2. Select **"Installed Dev Plugins"** from the dropdown or sidebar
3. You should see **"Chocobo Colourized"** in the list
4. Click the **checkbox** or **"Enable"** button next to it
5. The plugin should now load

**If you see an error:**
- Check that the DLL path is correct
- Verify the plugin compiled successfully (no build errors)
- Check the Dalamud log for specific error messages

---

## Step 7: Verify Plugin is Working

Test that the plugin loaded correctly:

1. Press **Enter** to open chat
2. Type: `/chococolor` (or whatever command the plugin uses)
3. Press **Enter**

If the plugin is working, you should see:
- The plugin's main window open, OR
- A response message in chat, OR
- Some indication that the command was recognized

**If nothing happens:**
- Check the Dalamud log for errors (`/xllog`)
- Verify the plugin is enabled in the Plugin Installer
- Try reloading the plugin (disable and re-enable it)

---

## Step 8: Using the Plugin

Once loaded and verified:

1. Use the command `/chococolor` to open the main interface
2. Follow the on-screen instructions to calculate chocobo color paths
3. Adjust settings via the configuration window if available

---

## Troubleshooting

### Plugin Doesn't Appear in Dev Plugins List

**Possible Causes:**
- DLL path is incorrect
- Plugin didn't compile successfully
- Plugin manifest (`.json` file) is missing or invalid

**Solutions:**
- Double-check the full path to the DLL
- Rebuild the plugin in your IDE
- Ensure `ChocoboColourized.json` exists next to the DLL

---

### Plugin Loads But Crashes Immediately

**Possible Causes:**
- Missing dependencies
- Code errors or exceptions
- Incompatible Dalamud API version

**Solutions:**
- Check the Dalamud log (`/xllog`) for error details
- Verify all required NuGet packages are installed
- Ensure plugin targets the correct .NET version (8.0)

---

### Plugin Command Not Recognized

**Possible Causes:**
- Plugin loaded but command registration failed
- Typo in command name
- Plugin is disabled

**Solutions:**
- Verify plugin is enabled in Plugin Installer
- Check the plugin code for the correct command name
- Try reloading the plugin

---

### Changes to Code Don't Appear In-Game

**Possible Causes:**
- Plugin wasn't rebuilt after changes
- Old DLL is cached
- Plugin wasn't reloaded

**Solutions:**
1. **Rebuild** the plugin in your IDE
2. **Disable** the plugin in Plugin Installer
3. **Wait** a few seconds
4. **Re-enable** the plugin
5. Alternatively, restart the game

---

## Updating the Plugin

When you make changes to the plugin code:

1. **Disable** the plugin in the Plugin Installer
2. **Close** the game (optional but recommended)
3. **Rebuild** the plugin in your IDE
4. **Restart** the game (if you closed it)
5. **Re-enable** the plugin in the Plugin Installer

**Note:** For minor changes, you can sometimes just disable/enable without restarting the game, but restarting is safer.

---

## Advanced: Auto-Load on Startup

To have the plugin load automatically when you start the game:

1. Open the Plugin Installer (`/xlplugins`)
2. Go to **Dev Tools > Installed Dev Plugins**
3. Find **Chocobo Colourized**
4. Look for an option like **"Load on Startup"** or similar
5. Enable it

Now the plugin will automatically load every time you launch FFXIV with Dalamud.

---

## Uninstalling the Plugin

To remove the plugin:

1. Open Plugin Installer (`/xlplugins`)
2. Go to **Dev Tools > Installed Dev Plugins**
3. Find **Chocobo Colourized**
4. Click **"Disable"** or **"Uninstall"**
5. (Optional) Remove the DLL path from Dalamud Settings > Experimental > Dev Plugin Locations

---

## Environment Variables (Advanced)

If XIVLauncher is installed in a non-standard location, you may need to set the `DALAMUD_HOME` environment variable:

**Windows:**
1. Search for "Environment Variables" in Windows search
2. Click "Edit the system environment variables"
3. Click "Environment Variables" button
4. Under "User variables", click "New"
5. Variable name: `DALAMUD_HOME`
6. Variable value: Path to your Dalamud dev directory
7. Click OK and restart your IDE

---

## Useful Commands Reference

| Command | Description |
|---------|-------------|
| `/xlsettings` | Open Dalamud settings |
| `/xlplugins` | Open Plugin Installer |
| `/xllog` | Open Dalamud log (for debugging) |
| `/xldev` | Open Dalamud developer menu |
| `/chococolor` | Open Chocobo Colourized plugin (once loaded) |

---

## Additional Resources

- **Dalamud Developer Docs:** https://dalamud.dev
- **Plugin Submission Guide:** https://dalamud.dev/plugin-publishing/submission
- **Dalamud Discord:** https://discord.gg/holdshift
- **SamplePlugin Repository:** https://github.com/goatcorp/SamplePlugin

---

## Summary Checklist

- [ ] XIVLauncher and Dalamud installed
- [ ] Plugin built successfully (DLL exists)
- [ ] Full path to DLL noted
- [ ] Game launched with Dalamud
- [ ] DLL path added to Dev Plugin Locations (`/xlsettings` > Experimental)
- [ ] Plugin enabled in Plugin Installer (`/xlplugins` > Dev Tools)
- [ ] Plugin command tested in chat
- [ ] Plugin functionality verified

---

**Congratulations!** You should now have the Chocobo Colourized plugin loaded and running in FFXIV. If you encounter any issues not covered in this guide, check the Dalamud log for detailed error messages.
