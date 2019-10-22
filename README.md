# SharpHide
Just a nice persistence trick to confuse DFIR investigation.
Uses NtSetValueKey native API to create a hidden (null terminated) registry key.
This works by adding a null byte in front of the UNICODE_STRING key valuename.

More info about this technique can be found in the following whitepaper:
https://github.com/ewhitehats/InvisiblePersistence/blob/master/InvisibleRegValues_Whitepaper.pdf

The tool uses the following registry path in which it creates the hidden run key:
(HKCU if user, else HKLM)\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"

## Usage
To Create hidden registry (Run) key:

```
SharpHide.exe action=create keyvalue="C:\Windows\Temp\Bla.exe" 
```

To Create a hidden registry (Run) key with parameters:

```
SharpHide.exe action=create keyvalue="C:\Windows\Temp\Bla.exe" arguments="arg1 arg2"
```

Delete hidden registry (Run) key:

```
SharpHide.exe action=delete
```

This tool also works with Cobalt Strike's execute-assembly.

## Credits
Author: Cornelis de Plaa (@Cneelis) / Outflank
