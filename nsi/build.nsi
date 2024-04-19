; Full script for making an NSIS installation package for .NET programs,
; Allows installing and uninstalling programs on Windows environment, and unlike the package system 
; integrated with Visual Studio, this one does not suck.

;To use this script:
;  1. Download NSIS (http://nsis.sourceforge.net/Download) and install
;  2. Save this script to your project and edit it to include files you want - and display text you want
;  3. Add something like the following into your post-build script (maybe only for Release configuration)
;        "$(DevEnvDir)..\..\..\NSIS\makensis.exe" "$(ProjectDir)Setup\setup.nsi"
;  4. Build your project. 
;
;  This package has been tested latest on Windows 7, Visual Studio 2010 or Visual C# Express 2010, should work on all older version too.

; Main constants - define following constants as you want them displayed in your installation wizard
!define PRODUCT_NAME "Vif Agent S7"
!define PRODUCT_VERSION "0.0.1"
!define PRODUCT_PUBLISHER "Adrien Clauzel."
!define PRODUCT_WEB_SITE "http://www.adclz.net"

; Following constants you should never change
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

!include "MUI.nsh"
!define MUI_ABORTWARNING
!define MUI_ICON "vif-logo.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Wizard pages
!insertmacro MUI_PAGE_WELCOME
; Note: you should create License.txt in the same folder as this file, or remove following line.
!insertmacro MUI_PAGE_LICENSE "Licence.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

; Replace the constants bellow to hit suite your project
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "SetupVifAgentS7_${PRODUCT_VERSION}.exe"
InstallDir "$PROGRAMFILES\Vif\Agents\S7"
ShowInstDetails show
ShowUnInstDetails show

; Following lists the files you want to include, go through this list carefully!
Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  File "..\bin\Release\net472\Vif-Agent-S7.exe"
  File "..\bin\Release\net472\Crayon.dll"
  File "..\bin\Release\net472\Newtonsoft.Json.dll"
  File "..\bin\Release\net472\Sharprompt.dll"
  File "..\bin\Release\net472\WatsonWebserver.dll"
  File "..\bin\Release\net472\WatsonWebserver.Core.dll"
  File "..\bin\Release\net472\IpMatcher.dll"
  File "..\bin\Release\net472\UrlMatcher.dll"
  File "..\bin\Release\net472\RegexMatcher.dll"
  File "..\bin\Release\net472\Microsoft.Bcl.AsyncInterfaces.dll"
  File "..\bin\Release\net472\Timestamps.dll"

; It is pretty clear what following line does: just rename the file name to your project startup executable.
  CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\Vif-Agent-S7.exe" ""
SectionEnd

Section -Post
  ;Following lines will make uninstaller work - do not change anything, unless you really want to.
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  
  WriteRegStr HKCR "vif-agent-s7" "" "URL:Vif Protocol"
  WriteRegStr HKCR "vif-agent-s7" "URL Protocol" ""
  WriteRegStr HKCR "vif-agent-s7\shell\open\command" "" '"$INSTDIR\Vif-Agent-S7.exe" "%1"'

SectionEnd

; Replace the following strings to suite your needs
Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "Application was successfully removed from your computer."
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove Vif Agent S7 and all of its components?" IDYES +2
  Abort
FunctionEnd

; Remove any file that you have added above - removing uninstallation and folders last.
Section Uninstall
  Delete "$INSTDIR\Vif-Agent-S7.exe"
  Delete "$INSTDIR\Crayon.dll"
  Delete "$INSTDIR\Newtonsoft.Json.dll"
  Delete "$INSTDIR\Sharprompt.dll"
  Delete "$INSTDIR\WatsonWebserver.dll"
  Delete "$INSTDIR\WatsonWebserver.Core.dll"
  Delete "$INSTDIR\UrlMatcher.dll"
  Delete "$INSTDIR\IpMatcher.dll"
  Delete "$INSTDIR\RegexMatcher.dll"
  Delete "$INSTDIR\Microsoft.Bcl.AsyncInterfaces.dll"
  Delete "$INSTDIR\Timestamps.dll"

  RMDir "$INSTDIR"
  RMDir "$INSTDIR\.."

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKCR "vif-agent-s7"

  SetAutoClose true
SectionEnd
