#ifndef AppVersion
  #error AppVersion must be supplied by Build-Installer.ps1
#endif
#ifndef PayloadDir
  #error PayloadDir must be supplied by Build-Installer.ps1
#endif
#ifndef OutputDir
  #error OutputDir must be supplied by Build-Installer.ps1
#endif
#ifdef TestBuild
  #define ProductId "GPTCompanion.Desktop.InstallerTest"
  #define ProductName "GPT 桌面伙伴安装测试"
  #define RunValueName "DragonCompanion.InstallerTest"
  #define OutputName "GC-v" + AppVersion + "-InstallerTest"
#else
  #define ProductId "GPTCompanion.Desktop"
  #define ProductName "GPT 桌面伙伴"
  #define RunValueName "DragonCompanion"
  #define OutputName "GC v" + AppVersion + " Setup"
#endif

[Setup]
AppId={#ProductId}
AppName={#ProductName}
AppVersion={#AppVersion}
AppVerName={#ProductName} v{#AppVersion}
AppPublisher=WodeFafa
AppCopyright=developed by WodeFafa
VersionInfoVersion={#AppVersion}.0
VersionInfoTextVersion={#AppVersion}.0
VersionInfoDescription=GPT 桌面伙伴安装程序
DefaultDirName={localappdata}\Programs\GPTCompanion
DefaultGroupName={#ProductName}
PrivilegesRequired=lowest
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
DisableDirPage=no
AllowRootDirectory=no
AppendDefaultDirName=no
UsePreviousAppDir=yes
UninstallDisplayIcon={app}\DragonCompanion.exe
UninstallDisplayName={#ProductName}
SetupIconFile=..\assets\app.ico
OutputDir={#OutputDir}
OutputBaseFilename={#OutputName}
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
WizardSizePercent=115,115
ShowLanguageDialog=no
CloseApplications=yes
RestartApplications=no
MinVersion=10.0
SetupLogging=no
UninstallLogging=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Messages]
SelectDirLabel3=选择安装位置，然后点击“一键安装”。更新与卸载都会保留个人计划、日程和词库。
SelectDirBrowseLabel=安装位置：
FinishedHeadingLabel=GPT 桌面伙伴已安装
FinishedLabelNoIcons=安装完成。你可以从开始菜单启动 GPT 桌面伙伴。
ExitSetupMessage=安装尚未完成。现在退出后，可以稍后重新运行安装程序。%n%n确认退出安装程序？%n%ndeveloped by WodeFafa
ConfirmUninstall=确定卸载 %1 及其程序组件？个人计划、日程和词库会保留。%n%ndeveloped by WodeFafa
UninstalledAll=已卸载 %1，个人数据已保留。%n%ndeveloped by WodeFafa

[Files]
Source: "{#PayloadDir}\DragonCompanion.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\更新日志.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\词库说明.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\RELEASE_NOTES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\vocabulary-data-source.zip"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\GPT 桌面伙伴"; Filename: "{app}\DragonCompanion.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#ProductName}"; Filename: "{app}\DragonCompanion.exe"; WorkingDir: "{app}"; Check: CreateDesktopShortcut

[Run]
Filename: "{app}\DragonCompanion.exe"; Description: "启动 GPT 桌面伙伴"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';
  Credit = 'developed by WodeFafa';
var
  DesktopCheck: TNewCheckBox;

function InstalledCommand: String;
begin
  Result := '"' + ExpandConstant('{app}\DragonCompanion.exe') + '" --background';
end;

function CreateDesktopShortcut: Boolean;
begin
  Result := Assigned(DesktopCheck) and DesktopCheck.Checked;
end;

function InitializeSetup: Boolean;
begin
  Result := True;
#ifndef TestBuild
  if CheckForMutexes('Local\DragonCompanion.' + GetUserNameString) then begin
    SuppressibleMsgBox('请先在 GPT 桌宠右键菜单选择“退出”，再运行安装程序。' + #13#10 +
      '你的个人计划、日程和词库会继续保留。' + #13#10#13#10 + Credit,
      mbInformation, MB_OK, IDOK);
    Result := False;
  end;
#endif
end;

procedure AddCredit(Owner: TComponent; Parent: TWinControl; Left, Top, Width: Integer);
var LabelControl: TNewStaticText;
begin
  LabelControl := TNewStaticText.Create(Owner);
  LabelControl.Parent := Parent;
  LabelControl.Left := Left;
  LabelControl.Top := Top;
  LabelControl.Width := Width;
  LabelControl.AutoSize := False;
  LabelControl.Caption := Credit;
  LabelControl.Font.Color := $009B6C88;
end;

procedure InitializeWizard;
begin
  WizardForm.Color := $00FCF8FB;
  WizardForm.PageNameLabel.Font.Color := $00984E78;
  DesktopCheck := TNewCheckBox.Create(WizardForm);
  DesktopCheck.Parent := WizardForm.SelectDirPage;
  DesktopCheck.Left := WizardForm.DirEdit.Left;
  DesktopCheck.Top := WizardForm.DirEdit.Top + WizardForm.DirEdit.Height + ScaleY(24);
  DesktopCheck.Width := WizardForm.SelectDirPage.ClientWidth;
  DesktopCheck.Caption := '创建桌面快捷方式';
  DesktopCheck.Checked := ExpandConstant('{param:DESKTOP|1}') <> '0';
  AddCredit(WizardForm, WizardForm, ScaleX(16),
    WizardForm.NextButton.Top + ScaleY(5), ScaleX(205));
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectDir then
    WizardForm.NextButton.Caption := '一键安装';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var InstallPath, DataPath: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then begin
    InstallPath := Uppercase(AddBackslash(ExpandFileName(WizardDirValue)));
    DataPath := Uppercase(AddBackslash(ExpandConstant('{localappdata}\DragonCompanion')));
    if Pos(DataPath, InstallPath) = 1 then begin
      MsgBox('请选择单独的程序安装文件夹，不能安装到个人数据目录。' + #13#10#13#10 + Credit, mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var PreviousCommand: String;
begin
  if CurStep = ssPostInstall then begin
    { Preserve opt-in: only update an existing enabled startup entry. }
    if RegQueryStringValue(HKCU, RunKey, '{#RunValueName}', PreviousCommand) then
      RegWriteStringValue(HKCU, RunKey, '{#RunValueName}', InstalledCommand);
  end;
end;

procedure InitializeUninstallProgressForm;
begin
  AddCredit(UninstallProgressForm, UninstallProgressForm,
    ScaleX(16), UninstallProgressForm.ClientHeight - ScaleY(22), ScaleX(230));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var CurrentCommand: String;
begin
  if CurUninstallStep = usPostUninstall then begin
    { Never remove a startup entry that the user repointed to another copy. }
    if RegQueryStringValue(HKCU, RunKey, '{#RunValueName}', CurrentCommand) and
      (CompareText(CurrentCommand, InstalledCommand) = 0) then
      RegDeleteValue(HKCU, RunKey, '{#RunValueName}');
  end;
end;
