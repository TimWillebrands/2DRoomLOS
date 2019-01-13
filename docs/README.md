# 2DRoomLOS

2D line-of-sight mesh generator and renderer for room based Unity2D games.
This generator works by checking the sightlines in a view origin room, then check what doors are visible from that point and generating the views trough whatever doors are visible. This approach seemed like a good idea to me because it limits the amount of raycasts there need to be done in one time, no need to check every corner point in the game, only the ones in the relevant room.
The system can draw as many line-of-sight mesheses as possible (in theory).

![Demo](demo.gif?raw=true)

I wrote this asset for a game I'm working on and it seemed like a nice idea to open source it. If you intend to use this code you should know that I am a hobbyist with no formal background in programming, therefor there are bugs and it is incredibly unlikely that the asset is as optimised as it could be.

I'm open to contributions to the project.
