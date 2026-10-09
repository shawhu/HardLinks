# HardLinks

A small Windows Forms tool for creating NTFS hard links with drag and drop.

## Releases

### v1.3

**Replace remembered folders by dragging a new folder onto a slot button.**

Each remembered-folder slot now accepts a direct drag-and-drop replacement: drag a single folder onto any filled slot button and it replaces that slot's target path immediately. The change is saved right away and the slot button updates to the new folder name without needing to clear or recreate the slot.

This makes it easy to update remembered targets in place while keeping the slot order and counts intact.

### v1.2

**Added post-creation checks**

Added post-creation checks using File.Exists for each link successfully created. The lblInfo result now ends with [verified] when all requested links were created and found, or [failed to verify] otherwise.

### v1.1

**Open in File Explorer from the folder buttons.**

Right-click any of the eight remembered-folder buttons to bring up a context menu with an **Open in File Explorer** option. It opens the folder that button points to, so you can check the result of a hard-link run or browse the target without leaving the app.

Empty slots are disabled and show no menu.

### v1.0

**Initial release.** Create hard links to one or more files inside a target folder, without typing paths or commands.

**How it works**

1. Drop one or more files onto the top drop zone. The app lists the files it accepted. Dropping again replaces the previous selection.
2. Choose the target folder in one of two ways:
   - Drop a single folder onto the "Drop ONE target folder here" zone.
   - Click one of the remembered-folder buttons.
3. Click **Create hard links in the target folder**. The button is enabled only when at least one file and a target folder are set.

Each hard link keeps the original file name. The info panel at the bottom reports the outcome: green when every link was created, red when some failed, together with the number created and the first error message.

**Remembered folders**

- Remembered target folders appear in the slot grid, labeled with the folder name and a recursive count of `.mp4`, `.mkv`, `.wmv`, and `.avi` files.
- Video counts refresh on startup, when the saved folder list changes, and after hard links are created. Counts are case-insensitive; unavailable folder scans are indicated in the slot and explained by its tooltip.
- Every time a folder is set as the target, its use count goes up, and the list is sorted by that count.
- The list is stored in `HardLinks.json` next to the executable and reloaded on startup.

**Notes**

- When all created link paths pass the existing post-creation check, the app plays a short, ascending C-major success flourish.
- When link creation fails or the existing post-creation check fails, the app plays a short, descending failure cue.
- Windows only. Links are created with the Win32 `CreateHardLink` call.
- A hard link must be on the same NTFS volume as the original file. Files on a different volume fail and are reported in the info panel.
- Only files can be linked, not folders.
- The window title shows the app version.
