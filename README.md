# Simple URP Auto Exposure
[![Website](https://img.shields.io/badge/Contact%20Website%20-8A2BE2)](https://miyu.gay)
[![Itch](https://img.shields.io/badge/Itch%20Profile-Itch?logo=Itch.io&color=gray)](https://miyulake.itch.io)

Feel free to use or modify this package!

![Scene Example](https://github.com/user-attachments/assets/35087fb7-de08-41cc-accd-ad5d1650616e)

## Requirements
- Unity 2022.3+
- Universal Render Pipeline (URP 14)

## Usage
- Install via [Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html) using Git URL.
```
https://github.com/miyulake/AutoExposureURP.git
```
- Add the Auto Exposure Renderer Feature to your Universal Renderer
- Create a Volume Profile with Auto Exposure Settings and adjust parameters to match your scene's lighting

**Renderer Feature**

![Render Feature Example](https://github.com/user-attachments/assets/c1806545-970e-478d-b78b-4b95d5c341cf)

**Volume Component**

![Volume Component Example](https://github.com/user-attachments/assets/aced3a93-3efb-43ff-9a11-441ca2ada8ee)

## Known Issues
- If no parameters are overridden in the volume component, the effect will be completely disabled.
- Currently no support for WebGL builds, as they can struggle with compute shaders (may work on WebGPU, but this has not been tested).

