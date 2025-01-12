# BlurInput

**BlurInput** is a Rainmeter plugin that provides a transparent and interactive input field for users. Designed for seamless integration into your Rainmeter skins, it allows for text input with various validation options and clipboard controls, all while maintaining a stylish and modern translucent appearance.

---

## Features 🚀

- 🌟 **Transparent Input Field**: A visually appealing input box with a blurred or translucent background.
- ⌨️ **Custom Input Types**:
  - Strings
  - Integers
  - Letters
  - Alphanumeric
  - Hexadecimal
  - Float
  - Email
  - Custom character sets
- 🔄 **Undo/Redo Support**: Easily revert or redo changes while typing.
- 📝 **Clipboard Operations**: Supports **Copy**, **Paste**, and **Cut** operations.
- ⚡ **User-Friendly Commands**: Trigger actions like starting/stopping input, clearing text, and showing a context menu via Rainmeter bangs.
- 🛡️ **Input Validation**: Ensures data integrity with built-in validators for different input types.
- ✨ **Customizable**: Supports character limits, dynamic input length, and text cursor behavior.

---

## Installation 📥

1. Download the latest release from the [Releases](https://github.com/NSTechBytes/BlurInput/releases) section.
2. Copy the `BlurInput.dll` file to the **Plugins** folder of your Rainmeter installation:
   ```
   C:\Program Files\Rainmeter\Plugins
   ```
3. Add the `BlurInput` plugin to your Rainmeter skin.

---

## Usage 🛠️

To integrate the **BlurInput** plugin into your Rainmeter skin, use the following template:

### Basic Example

```ini
[Rainmeter]
Update=0

[MeasureInput]
Measure=Plugin
Plugin=BlurInput
MeterName=MeterInputField
Cursor=|
FormatMultiline=1  ;This will format the multiline text into one line.
Password= (0,1)
Multiline= (0,1) ;When Using Multiline Use CTRL+ENTER for submit Text otherwise use only ENTER.
Limit= (0 for not Limit fix)
Width= (0 for not Width fix)
InputType=String  (String,Integer,Float,Letters,Alphanumeric,Hexadecimal,Email,Custom) any one
CharacterLimit=50
DefaultValue=Type here...
SkinUnFocusDismiss=1
DismissAction=[!Log "Dismiss"]
OnEnterAction=[!Log "Log:[InputHandler]"]
OnESCAction=[!Log "[InputHandler]"]
DynamicVariables=1

[MeterInputField]
Meter=String
Text=
X=20
Y=50
W=200
H=30
FontSize=12


```

---

### Plugin Bangs (Commands)

The following bangs can be used to interact with the input field:

| Bang                            | Description                     |
| ------------------------------- | ------------------------------- |
| `!CommandMeasure "start"`     | Starts the input field.         |
| `!CommandMeasure "stop"`      | Stops the input field.          |
| `!CommandMeasure "cleartext"` | Clears all text in the field.   |
| `!CommandMeasure "copy"`      | Copies text to the clipboard.   |
| `!CommandMeasure "paste"`     | Pastes text from the clipboard. |
| `!CommandMeasure "cut"`       | Cuts text to the clipboard.     |
| `!CommandMeasure "redo"`      | Redoes the last action.         |
| `!CommandMeasure "undo"`      | Undoes the last action.         |
| `!CommandMeasure "context"`   | Opens the context menu.         |

---

### Custom Input Validation 🛡️

You can validate input with specific types:

- **String**: Allows all characters.
- **Integer**: Allows numbers and a negative sign at the start.
- **Letters**: Accepts only alphabetical characters.
- **Alphanumeric**: Accepts letters and numbers.
- **Hexadecimal**: Allows hexadecimal characters (A-F, 0-9).
- **Float**: Accepts floating-point numbers.
- **Email**: Ensures valid email format.
- **Custom**: Use the `AllowedCharacters` property to specify accepted characters.

Example:

```ini
InputType=Custom
AllowedCharacters=abc123
```

---

## General Keyboard Actions

| **Action**      | **Key** | **Description**                                                                     |
| --------------------- | ------------- | ----------------------------------------------------------------------------------------- |
| **Backspace**   | 8             | Removes the character before the cursor position.                                         |
| **Delete**      | 46            | Removes the character at the cursor position.                                             |
| **Enter**       | 13            | Submits the input (if not in multiline mode) or inserts a newline (if in multiline mode). |
| **Esc**         | 27            | Executes the assigned `OnESCAction` and stops input.                                    |
| **Arrow Left**  | 37            | Moves the cursor one position to the left.                                                |
| **Arrow Right** | 39            | Moves the cursor one position to the right.                                               |
| **Home**        | 36            | Moves the cursor to the beginning of the text.                                            |
| **End**         | 35            | Moves the cursor to the end of the text.                                                  |
| **Tab**         | 9             | Inserts a tab space at the current cursor position.                                       |
| **Caps Lock**   | 20            | Toggles the Caps Lock state.                                                              |

## Ctrl + Keyboard Shortcuts

These shortcuts require the **Ctrl** key to be pressed along with a specific key:

| **Action**          | **Shortcut** | **Description**                                                                                                 |
| ------------------------- | ------------------ | --------------------------------------------------------------------------------------------------------------------- |
| **Copy**            | Ctrl + C           | Copy the current text to the clipboard.                                                                               |
| **Paste**           | Ctrl + V           | Paste text from the clipboard.                                                                                        |
| **Cut**             | Ctrl + X           | Cut the current text to the clipboard.                                                                                |
| **Undo**            | Ctrl + Z           | Undo the last change.                                                                                                 |
| **Redo**            | Ctrl + Y           | Redo the last undone change.                                                                                          |
| **Execute OnEnter** | Ctrl + Enter       | Executes the action assigned to the `OnEnterAction` only when `Multiline=1`, otherwise uses only **Enter**. |

## Contributing 🤝

Contributions are welcome! If you have ideas for enhancements, bug fixes, or additional features, please submit an issue or pull request.

---

## License 📄

This project is licensed under the Apache License. See the [LICENSE](LICENSE) file for details.

---

## Credits 💡

- Developed by **[Nasir Shahbaz]**.
- Inspired by the need for a cleaner, transparent input field in Rainmeter.

---

Enjoy using **BlurInput** and make your Rainmeter skins more interactive and stylish! 🎨
