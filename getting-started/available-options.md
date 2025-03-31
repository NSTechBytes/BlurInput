---
icon: bagel
---

# Available Options

## BlurInput Plugin Configuration Parameters

Each parameter is detailed below with its default value, requirement status, and a description.

***

### MeterName

* **Default Value:** None
* **Requirement:** Must Provide
* **Description:**\
  The `MeterName` is a mandatory configuration parameter. It specifies the name of the Rainmeter meter that will visually display the text input managed by the plugin. The meter’s text can be updated dynamically with the raw input, a masked version (for passwords), or a version modified using substitution rules.

***

### Cursor

* **Default Value:** `|`
* **Requirement:** Optional
* **Description:**\
  The Cursor marks the location where the next character will be inserted or deleted within the text buffer. By default, it is represented by a vertical bar but can be customized.

***

### Password

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When enabled (`Password=1`), the text entered by the user is masked in the meter. Instead of displaying the actual characters, each is replaced with a `*`.

***

### Multiline

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When enabled (`Multiline=1`), the plugin treats the text input as multi-line, allowing users to insert line breaks using the `Enter` key. The text buffer will handle multiple lines accordingly.

***

### FormatMultiline

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When set to `FormatMultiline=1`, all line breaks (`\r` or ) in the TextBuffer are replaced with a single space, effectively flattening the text into one line. If disabled, the original multi-line format is retained.

***

### InputLimit

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  If set to a positive integer (e.g., `InputLimit=50`), this parameter limits the maximum number of characters that can be entered into the text buffer. Any characters entered beyond this limit are ignored.

***

### ViewLimit

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  This option controls how much of the input text is visible in the meter, defining the maximum width of the text displayed. When `ViewLimit=0`, the entire TextBuffer is displayed without truncation.

***

### DefaultValue

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Defines the default text shown in the input field when it is first loaded or cleared. This value often serves as a hint, example, or placeholder to guide the user.

***

### InputType

* **Default Value:** String
* **Requirement:** Optional
* **Description:**\
  Specifies the type of data the input field accepts. It determines the kind of validation applied to the input, allowing only the specified type of data. Predefined types include `String`, `Integer`, `Float`, `Letters`, `Alphanumeric`, `Hexadecimal`, and `Email`, with an option for `Custom` validation.

***

### AllowedCharacters

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Defines a list of allowed characters when `InputType` is set to `"Custom"`. For instance, to allow only numeric digits and letters (both cases), you can set it to `"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"`.

***

### UnFocusDismiss

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When set to `1`, the input field is automatically dismissed when it loses focus. If set to `0`, the field remains visible and active even when focus is lost.

***

### ShowErrorDialog

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  If enabled (`ShowErrorDialog=1`), the plugin displays a dialog box with an error message when invalid input is entered or an error condition occurs. This dialog appears after text submission.

***

### SetInActiveValue

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When `ShowErrorDialog` is enabled and this parameter is set, the plugin sets the value of `InActiveValue` instead of `DefaultValue` when the input field is dismissed or another action is performed.

***

### InActiveValue

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Specifies the text value used when the input field is inactive. This text is defined by this option.

***

### OnDismissAction

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Defines the action or behavior that occurs when the input field is dismissed or loses focus (for example, when the user clicks away).

***

### OnEnterAction

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Specifies the action triggered when the user presses the `Enter` key while interacting with the input field. This can be used for submitting the input, executing a script, or shifting focus.

***

### OnInvalidAction

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  When invalid data is entered (such as text in a field that only allows integers), this action is triggered after text submission.

***

### OnESCAction

* **Default Value:** Null
* **Requirement:** Optional
* **Description:**\
  Defines the behavior that occurs when the user presses the `ESC` key while interacting with the input field. This can be used for actions like dismissing the input or triggering a script.

***

### ForceValidInput

* **Default Value:** 0
* **Requirement:** Optional
* **Description:**\
  When set to `1`, strict validation rules are enforced based on the `InputType`. For example, if `InputType` is `Integer`, only digits (and possibly a negative sign at the start) are allowed. Invalid characters will be rejected.

***

### FormBackgroundColor

* **Default Value:** 30,30,30
* **Requirement:** Optional
* **Description:**\
  Specifies the background color for the form in both the `ContextForm` and the `ErrorDialog` class.

***

### FormButtonColor

* **Default Value:** 70,70,70
* **Requirement:** Optional
* **Description:**\
  Defines the color of the buttons on the form in the `ContextForm` and the `ErrorDialog` class.

***

### FormTextColor

* **Default Value:** 255,255,255
* **Requirement:** Optional
* **Description:**\
  Determines the text color used in the form for both the `ContextForm` and the `ErrorDialog` class.

***

This layout separates each parameter clearly, allowing you to quickly locate and understand the configuration options without the constraints of a table format.
