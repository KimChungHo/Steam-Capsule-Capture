# Steam Capsule Capture

## 한국어

Unity Game 뷰에서 원하는 영역을 선택하고 Steam 이미지 규격에 맞는 PNG로 저장합니다.

1. Unity 상단 메뉴에서 **SteamTool > Steam Capsule Capture**를 엽니다.
2. 캡처할 게임 화면이 렌더되는 **Game** 탭을 표시합니다.
3. 도구 창에서 저장할 이미지 종류를 선택합니다.
4. 빨간 사각형의 **꼭짓점**을 끌어 크기를 조절합니다. 가로세로 비율은 유지됩니다.
5. 사각형 **내부**를 끌어 캡처할 위치로 옮깁니다.
6. **선택 영역 PNG 저장**을 누릅니다. 선택한 이미지 종류의 해상도로 저장됩니다.
7. **마지막 저장 파일 보기**를 눌러 저장된 파일 위치를 엽니다.

PNG는 Unity 프로젝트 최상단의 **Screenshots** 폴더에 저장됩니다. 폴더가 없으면 자동 생성하며, 저장할 때마다 새 파일을 만듭니다.

- **한국어 / English**: 토글을 켜면 영어 UI로 바뀝니다. 실행 메뉴는 항상 `SteamTool > Steam Capsule Capture`입니다.
- **선택 영역 초기화**: 현재 이미지 비율에 맞춰 영역을 초기화합니다. 다른 이미지 종류를 선택해도 영역이 초기화됩니다.
- **Game 뷰 가이드 표시**: 게임을 마우스로 조작하려면 끄세요. 켜져 있으면 사각형 내부의 마우스 입력은 영역 이동에 사용됩니다.
- **Undo/Redo**: 이미지 종류 선택과 영역 변경을 되돌리거나 다시 적용합니다.

게임 UI와 배경은 저장되며, 빨간 가이드와 에디터 UI는 저장되지 않습니다. Gizmos가 보이면 캡처 전에 끄세요. 선택 영역이 출력보다 작다는 안내가 나오면 Game 뷰 렌더 해상도를 높여 더 선명하게 저장할 수 있습니다. 가이드가 화면 밖으로 잘리면 Game 뷰의 **Scale**을 낮추세요.

저장 버튼이 비활성화되면 Game 탭을 표시하고 게임 화면이 렌더되었는지 확인하세요. 컴파일이나 플레이 모드 전환 중에는 완료될 때까지 기다리세요. 마지막 렌더 프레임이 있으면 편집 모드와 일시 정지 상태에서도 저장할 수 있습니다.

## English

Select an area in the Unity Game view and save it as a PNG at the selected Steam image dimensions.

1. Open **SteamTool > Steam Capsule Capture** from the Unity menu bar.
2. Display the **Game** tab with the game image you want to capture rendered in it.
3. Select the image type in the tool window.
4. Drag a **corner** of the red rectangle to resize it. The aspect ratio stays fixed.
5. Drag **inside** the rectangle to position the crop.
6. Click **Save crop as PNG**. The image is saved at the selected preset's resolution.
7. Click **Reveal last saved file** to open the saved file's location.

PNGs are saved in the **Screenshots** folder at the root of your Unity project. The folder is created automatically if needed, and each save creates a new file.

- **한국어 / English**: Enable this toggle to switch to English UI. The menu path is always `SteamTool > Steam Capsule Capture`.
- **Reset crop**: Reset the selection to the current image aspect ratio. Selecting another image type also resets the crop.
- **Show Game view guide**: Disable this to interact with the game using the mouse. When enabled, mouse input inside the rectangle moves the crop.
- **Undo/Redo**: Undo or redo image type selections and crop changes.

The capture includes the in-game UI and background, but excludes the red guide and editor UI. Turn off Gizmos before capturing if they are visible. If the tool warns that the crop will be upscaled, increase the Game view render resolution for sharper output. Reduce Game view **Scale** if the guide is clipped by the window.

If the save button is disabled, display the Game tab and make sure it has rendered an image. Wait for compilation or Play Mode transitions to finish. You can also save in Edit Mode or while paused if a last rendered frame is available.
