---
icon: input-text
---

# Custom Input Validation&#x20;



You can validate input with specific types:

* **String**: Allows all characters.
* **Integer**: Allows numbers and a negative sign at the start.
* **Letters**: Accepts only alphabetical characters.
* **Alphanumeric**: Accepts letters and numbers.
* **Hexadecimal**: Allows hexadecimal characters (A-F, 0-9).
* **Float**: Accepts floating-point numbers.
* **Email**: Ensures valid email format.
* **Custom**: Use the `AllowedCharacters` property to specify accepted characters.

Example:

```ini
InputType=Custom
AllowedCharacters=abc123
```
