# BlurInput

**BlurInput** is a Rainmeter plugin that provides a transparent and interactive input field for users. Designed for seamless integration into your Rainmeter skins, it allows for text input with various validation options and clipboard controls, all while maintaining a stylish and modern translucent appearance.

 ### DOcumentaton
 https://nstechbytes.gitbook.io/blurinput/

## Usage 🛠️

To integrate the **BlurInput** plugin into your Rainmeter skin, use the following template:

### Basic Example

```ini
[Rainmeter]
Update=1000

[Metadata]
Name=BlurInput Test Skin
Author=NS TEch Bytes
Information=Usage example to use plugin.
Version=Version 1.5
License=Creative Commons BY-NC-SA 3.0

[Variables]
Text=Type Here

;===================================================================================================================;
[InputHandler]
Measure=Plugin
Plugin=BlurInput
MeterName=Text_1
;Cursor=|
;Password=1   
Multiline=1 
;FormatMultiline=1
;InputLimit=0   
;ViewLimit=0   
DefaultValue=#Text#
InputType=String
;AllowedCharacters=abc
UnFocusDismiss=1
;ShowErrorDialog=1
;FormBackgroundColor=255,250,250
;FormButtonColor=136,132,132
;FormTextColor=6,6,6
;SetInActiveValue=0
;InActiveValue=Any Value
OnDismissAction=[!Log "Dismiss"]
;ForceValidInput=1
OnEnterAction=[!Log """[InputHandler]"""][!WriteKeyValue Variables Text """[InputHandler]"""]
OnInvalidAction=[!Log  "UnValidInput Type" "Debug"]
OnESCAction=[!Log """[InputHandler]"""]
DynamicVariables=1
;RegExpSubstitute=1
;Substitute="\n":"#*CRLF*#"
;===================================================================================================================;

[BackGround]
Meter=Shape 
Shape=Rectangle 0,0,800,400,8 |StrokeWidth 0 | FillColor 22,22,22,200
DynamicVariables=1

[Text_1]
Meter=String
Text=#Text#
FontSize=22
FontColor=255,255,255
X=10
Y=10
Antialias=1
FontWeight=200
stringAlign = Left
FOntFace=Arial
DynamicVariables=1
LeftMouseUpAction=[!CommandMeasure InputHandler "Start"]




```

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
