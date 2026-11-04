; Who Broke It? — Windows kurulum sihirbazı (NSIS 3, Unicode)
; Derleme: makensis -DVERSION=0.3.0 -DSTAGE=/stage wbi.nsi      (tools/release.sh çağırır)
Unicode true
SetCompressor /SOLID lzma

!ifndef VERSION
  !define VERSION "0.0.0"
!endif
!ifndef STAGE
  !define STAGE "stage"
!endif

!define APP_NAME   "Who Broke It?"
!define COMPANY    "CiciByte"
!define APP_DIR    "WhoBrokeIt"
!define APP_EXE    "WhoBrokeIt.exe"
!define REG_APP    "Software\CiciByte\WhoBrokeIt"
!define REG_UNINST "Software\Microsoft\Windows\CurrentVersion\Uninstall\WhoBrokeIt"

Name "${APP_NAME}"
OutFile "WhoBrokeIt-Setup-${VERSION}.exe"
; Varsayılan: C:\Program Files\CiciByte\WhoBrokeIt
InstallDir "$PROGRAMFILES64\${COMPANY}\${APP_DIR}"
InstallDirRegKey HKLM "${REG_APP}" "InstallDir"
RequestExecutionLevel admin
BrandingText "${COMPANY} · ${APP_NAME} ${VERSION}"

VIProductVersion "${VERSION}.0"
VIAddVersionKey /LANG=1055 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=1055 "CompanyName" "${COMPANY}"
VIAddVersionKey /LANG=1055 "FileDescription" "${APP_NAME} kurulumu"
VIAddVersionKey /LANG=1055 "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=1055 "LegalCopyright" "© 2026 ${COMPANY}"

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "${APP_NAME} oyununu başlat"
!define MUI_FINISHPAGE_NOREBOOTSUPPORT

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Turkish"
!insertmacro MUI_LANGUAGE "English"

LangString DESC_Main ${LANG_TURKISH} "Oyunun kendisi (gerekli)."
LangString DESC_Main ${LANG_ENGLISH} "The game itself (required)."
LangString DESC_Desktop ${LANG_TURKISH} "Masaüstüne kısayol ekle."
LangString DESC_Desktop ${LANG_ENGLISH} "Add a desktop shortcut."

Function .onInit
  SetRegView 64
  ; çalışan oyun dosyaları kilitlerse kurulum yarıda kalmasın
  nsExec::Exec 'taskkill /F /IM ${APP_EXE}'
  Pop $0
FunctionEnd

Section "${APP_NAME} (gerekli)" SecMain
  SectionIn RO
  SetRegView 64
  SetOutPath "$INSTDIR"
  File /r "${STAGE}/${APP_DIR}/*"

  ; Uninstall.exe ayrıca oyunun "kurulu mu?" denetiminde kullanılır
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Oyun kendini güncellerken yönetici izni istememesi için, yalnızca bu oyun klasörüne kullanıcılar için yazma izni ver
  nsExec::ExecToLog 'icacls "$INSTDIR" /grant *S-1-5-32-545:(OI)(CI)M /T /C /Q'
  Pop $0

  WriteRegStr HKLM "${REG_APP}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${REG_UNINST}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKLM "${REG_UNINST}" "Publisher" "${COMPANY}"
  WriteRegStr HKLM "${REG_UNINST}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${REG_UNINST}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  WriteRegStr HKLM "${REG_UNINST}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${REG_UNINST}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegStr HKLM "${REG_UNINST}" "URLInfoAbout" "https://whobrokeit.cicibyte.com/"
  WriteRegDWORD HKLM "${REG_UNINST}" "NoModify" 1
  WriteRegDWORD HKLM "${REG_UNINST}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${REG_UNINST}" "EstimatedSize" "$0"

  CreateDirectory "$SMPROGRAMS\${COMPANY}"
  CreateShortcut "$SMPROGRAMS\${COMPANY}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
  CreateShortcut "$SMPROGRAMS\${COMPANY}\${APP_NAME} Kaldır.lnk" "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Masaüstü kısayolu" SecDesktop
  SetRegView 64
  CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_Main)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_Desktop)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onInit
  SetRegView 64
  nsExec::Exec 'taskkill /F /IM ${APP_EXE}'
  Pop $0
FunctionEnd

Section "Uninstall"
  SetRegView 64
  Delete "$DESKTOP\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${COMPANY}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${COMPANY}\${APP_NAME} Kaldır.lnk"
  RMDir "$SMPROGRAMS\${COMPANY}"
  RMDir /r "$INSTDIR"
  RMDir "$PROGRAMFILES64\${COMPANY}"
  DeleteRegKey HKLM "${REG_UNINST}"
  DeleteRegKey HKLM "${REG_APP}"
  DeleteRegKey /ifempty HKLM "Software\${COMPANY}"
SectionEnd
