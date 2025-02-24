## Updates:
- Add Low mode (Very small texture size, normalized positions and normals [0..1], TextureFormat.RGBA32)

# Unity VAT Baker ![GitHub License](https://img.shields.io/github/license/Mamantenok1599/UnityVATBaker)

This repository contains a tool for baking Vertex Animation Textures (VAT) directly within Unity. The tool is currently focused on working with SkinnedMeshRenderer components (primarily characters).


<p align="center">
  <img width="80%" src="https://s13.gifyu.com/images/SJkTV.gif" />
</p>
<p align="center">
  Examples showing different animation step sizes
</p>

**Important Note:**  
This tool still has areas that could be improved, such as adding more automation to the baking process or optimizing how data is packed. I may address these enhancements in the future.

<p align="center">
  <img src="https://i.giphy.com/media/v1.Y2lkPTc5MGI3NjExaXAwbnE0bmp1M3FkbWlwZWhiMm9jeHo3bnE0dG1xZ214cmx5dXV1MCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/sf4kgxZydjZNvNttjT/giphy-downsized-large.gif" />
  <img src="https://i.giphy.com/media/v1.Y2lkPTc5MGI3NjExNjVkMTZyMmFoeWhleTdjYXo1aDN0MG5ncnAyZW15aThkbW8xaGV5ciZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/ctJoBshd8q14WbeJE8/giphy-downsized-large.gif" />
</p>

<p align="center">
  A sample scene for comparisons is included in the project
</p>

## System requirements
- Unity 6 and above.  
  *(The tool might also work in older versions that support Shader Graph, but this hasn't been tested.)*

## Tool Features
- Localization into Russian, English, Japanese, and Simplified Chinese (I wanted to practice adding localization. :smiley:)
- Prefab validation checks and helpful warnings
- Ability to gather all animation clips from the Animator Controller into a list
- Prefab preview with a selected animation clip
- Adjustable frame step (to reduce frame count and texture size, with minimal quality loss)
- No vertex count limit (the tool uses multiple rows per frame)
- Option to bake positions and normals
- Two Shader Graph subgraphs included:
  - **VAT_Simple**: Uses 2 texture samplers
  - **VAT_WithFrameBlend**: Uses 4 texture samplers and simple blending logic
- Example scene included for:
  - Comparing FrameStep and Blend modes
  - Performance testing with 1000+ Animator Controllers vs. GPU instancing using VAT
    
## How to Use the VAT Baker
**1.** Open the tool window: **Tools/VatBaker**
   
<p align="center">
  <img src="https://s13.gifyu.com/images/SJkEm.png" />
</p>

**2.** Assign a prefab with a SkinnedMeshRenderer and an Animator Controller containing animations

<p align="center">
  <img src="https://s7.gifyu.com/images/SJkEg.png" />
</p>

**3.** Select an animation clip from the list (all clips in the Animator Controller are listed)

<p align="center">
  <img src="https://s13.gifyu.com/images/SJkE0.png" />
</p>

**4.** Adjust the animation length and frame step.  
Frame step determines how many frames are skipped between baked frames, reducing the final texture size while preserving most of the animation quality. This is especially useful for simple animations.
<p align="center">
  <img src="https://s7.gifyu.com/images/SJkEf.png" />
</p>

**5.** Choose the texture width (height is computed automatically and rounded up to a power-of-two). You can skip baking normals if they're not needed, and specify the output path and filename. The tool will append the mesh name, animation clip name, and data type to the filename.
<p align="center">
  <img src="https://s13.gifyu.com/images/SJkEe.png" />
</p>

**6.** You'll need to manually adjust the textures using a preset or by editing their import settings as shown below:
<p align="center">
  <img width="50%" src="https://s7.gifyu.com/images/SJkhl.png" />
</p>

<p align="center">
  <img width="50%" src="https://s7.gifyu.com/images/SJkhu.png" />
</p>

<p align="center">
  <img width="50%" src="https://s7.gifyu.com/images/SJkhW.png" />
</p>

**7.** Create a material and assign `VAT_Debug` shader (for example)

<p align="center">
  <img src="https://s13.gifyu.com/images/SJk7g.png" />
</p>

**8.** Set up the material as follows (in the future, I'll automate this):
  1. **Total Frames** = source frame count / frame step (e.g., 60/4 = 15)
  2. **Frame Rate** = source frame rate / frame step (e.g., 24/4 = 6), or adjust to control animation playback speed
  3. **Vertex Count** can be found in the tool window
  4. **Texture Width/Height** matches the baked texture
  5. **Rows Per Frame** = ceil(VertexCount / TextureWidth). For example, if 2204/1024 ≈ 2.15 => 3 rows
  6. Compare different sampling modes as desired.
      
<p align="center">
  <img src="https://s7.gifyu.com/images/SJk70.png" />
</p>

<p align="center">
  <img src="https://s13.gifyu.com/images/SJkEL.gif" />
</p>

<p align="center">
  Result
</p>

## Shader Implementation
To integrate VAT in your shader, simply select one of the provided subgraphs (`VAT_Simple` or `VAT_WithFrameBlend`) and connect the output positions and normals to your shader's vertex stage. The HLSL version will be made available later.

<p align="center">
  <img src="https://s7.gifyu.com/images/SJklB.png" />
</p>

<p align="center">
  <img src="https://s7.gifyu.com/images/SJknw.png" />
</p>

<p align="center">
  <img src="https://s7.gifyu.com/images/SJkn9.png" />
</p>

## When will this be updated?

If enough people show interest in this tool, I plan to make further improvements.

## It's not working for me. Can you help?

Please leave your feedback. When I have some free time, I'll do my best to address any issues and improve the tool.

## Can I use this tool freely?

Absolutely! Feel free to use it in your projects, including commercial ones. You can modify and improve it as you like. However, please do not sell or redistribute the asset itself as a standalone product—it’s meant to remain free.
