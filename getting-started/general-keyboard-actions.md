---
icon: keyboard
---

# General Keyboard Actions

| **Action**      | **Key** | **Description**                                                                           |
| --------------- | ------- | ----------------------------------------------------------------------------------------- |
| **Backspace**   | 8       | Removes the character before the cursor position.                                         |
| **Delete**      | 46      | Removes the character at the cursor position.                                             |
| **Enter**       | 13      | Submits the input (if not in multiline mode) or inserts a newline (if in multiline mode). |
| **Esc**         | 27      | Executes the assigned `OnESCAction` and stops input.                                      |
| **Arrow Left**  | 37      | Moves the cursor one position to the left.                                                |
| **Arrow Right** | 39      | Moves the cursor one position to the right.                                               |
| **Home**        | 36      | Moves the cursor to the beginning of the text.                                            |
| **End**         | 35      | Moves the cursor to the end of the text.                                                  |
| **Tab**         | 9       | Inserts a tab space at the current cursor position.                                       |
| **Caps Lock**   | 20      | Toggles the Caps Lock state.                                                              |

### Ctrl + Keyboard Shortcuts

These shortcuts require the **Ctrl** key to be pressed along with a specific key:

| **Action**          | **Shortcut** | **Description**                                                                                             |
| ------------------- | ------------ | ----------------------------------------------------------------------------------------------------------- |
| **Copy**            | Ctrl + C     | Copy the current text to the clipboard.                                                                     |
| **Paste**           | Ctrl + V     | Paste text from the clipboard.                                                                              |
| **Cut**             | Ctrl + X     | Cut the current text to the clipboard.                                                                      |
| **Undo**            | Ctrl + Z     | Undo the last change.                                                                                       |
| **Redo**            | Ctrl + Y     | Redo the last undone change.                                                                                |
| **Execute OnEnter** | Ctrl + Enter | Executes the action assigned to the `OnEnterAction` only when `Multiline=1`, otherwise uses only **Enter**. |
