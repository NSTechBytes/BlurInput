---
icon: flag-usa
---

# Usage&#x20;

To integrate the **BlurInput** plugin into your Rainmeter skin, use the following template:

#### Basic Example

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
