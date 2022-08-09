
## <p align=center><img src="https://i.imgur.com/TLrwH0y.png" width="65" height="65" /><div align="center">FITS Rating Tool</div></span></p>

### &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;FITS Viewer
![](https://i.imgur.com/eR9jIM7.png)
---
### <p align=right>Evaluation Tools&nbsp;&nbsp;&nbsp;&nbsp;</p>
![](https://i.imgur.com/1G7Qajw.png)
---
### &nbsp;&nbsp;Automation with Jobs and CLI
![](https://i.imgur.com/wJjhTv9.png) 
---
### Features
- FITS viewer with auto-stretch, rudimentary RGB/debayer support, corner/aberration viewer and peek viewer
- Star detection and analysis using [SEP](https://github.com/kbarbary/sep)
- Image statistics (Median, FWHM, HFR, Eccentricity, etc.)
- Image grading with custom expressions
- Jobs and CLI for automated image grading
- Integration with [Starkeeper Voyager](https://software.starkeeper.it/) and its [RoboTarget](https://software.starkeeper.it/voyager-advanced/) database
---
### TODOs
- [ ] Installer
- [ ] Settings
- [ ] Improve RGB/debayer support and allow selecting which color channel is used for the image analysis 
- [ ] More exporters (e.g. an exporter that moves files to another directory based on its rating)
- [ ] Allow manually setting/overwriting ratings of specific files
- [ ] Reduce memory usage
- [ ] Linux & macOS support
---
### Copyright
<pre>
FITS Rating Tool
Copyright (C) 2022 TheCyberBrick

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see &lt;https://www.gnu.org/licenses/&gt;.
</pre>
The GPL and third-party licenses can be found [here](GuiApp/Resources).