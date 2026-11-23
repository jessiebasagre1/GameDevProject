HORROR GAME ASSETS - Shotgun + Blueprint
=========================================

Shotgun/Shotgun.fbx
  Double-barrel break-action shotgun with a bone rig (skinned meshes).
  Bones: Root > Gun > Barrels / Lever / Trigger ; Barrels > ShellL / ShellR / Muzzle
  Clips (30 fps): Idle (loop, 3 s), Draw (0.8 s), Fire (0.6 s), DryFire (0.33 s), Reload (2.2 s)
  The muzzle flash is the mesh "Shotgun_MuzzleFlash". It is scaled to ~0 except during Fire.
  Muzzle points toward the -Y side in Blender (= +Z forward in Unity). If it points backward, rotate the model 180 deg on Y.

Blueprint/Blueprint_01_Barrels.fbx ... Blueprint_04_Assembly.fbx
  The same paper-sheet pickup with 4 different drawings (barrels, receiver, stock, assembly).
  Clips (30 fps): Float (loop, 4 s: spin, bob, glow pulse), Collect (0.6 s: rise, spin, shrink).
  Blueprint_Glow.png = soft glow card texture (use a Transparent / Additive material).

Previews/  = quick renders so you can see what you are getting.
HorrorGameAssets.blend = the editable Blender file with everything.

UNITY IMPORT
  1. Copy Shotgun/ and Blueprint/ into Assets/ (keep the PNG files next to the FBX files).
  2. FBX > Rig tab: Animation Type = Generic. Animation tab: tick Loop Time on Idle and Float only.
  3. Materials tab: Extract Materials / Textures. Set Blueprint_Glow to Transparent (or Particles/Additive).
     Muzzle flash material: tick Emission (orange) so it glows.
