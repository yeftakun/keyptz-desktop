#define MyAppName "keyPTZ-desktop"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Yeftakun"
#define MyAppExeName "keyPTZ-desktop.exe"
#define MySourceDir "release\win-x64-small-safe"
#define MyAppDataDir "{localappdata}\keyPTZ-desktop"
#define ViGEmBusUrl "https://github.com/nefarius/ViGEmBus/releases"
#define ViGEmInstallerFile "ViGEmBusSetup.exe"

[Setup]
AppId={{E6F8A5A2-5E4A-4E88-9C17-DF4DFA8D6C01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=release\installer
OutputBaseFilename={#MyAppName}-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut Desktop"; GroupDescription: "Shortcut:"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "prereq\{#ViGEmInstallerFile}"; Flags: dontcopy

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ViGEmPage: TWizardPage;
  ViGEmInstalledCheckBox: TNewCheckBox;
  ViGEmAutoInstallCheckBox: TNewCheckBox;
  ViGEmLinkLabel: TNewStaticText;
  ViGEmStatusLabel: TNewStaticText;
  ViGEmRecheckButton: TNewButton;

  DeleteUserDataOnUninstall: Boolean;

function IsViGEmBusInstalled: Boolean;
begin
  Result :=
    RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\ViGEmBus') or
    RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\ViGEmBus');
end;

function GetViGEmInstallerPath: string;
begin
  Result := ExpandConstant('{tmp}\{#ViGEmInstallerFile}');
end;

procedure OpenViGEmBusLink(Sender: TObject);
var
  ErrorCode: Integer;
begin
  ShellExec('open', '{#ViGEmBusUrl}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure UpdateViGEmStatus;
begin
  if IsViGEmBusInstalled then
  begin
    ViGEmStatusLabel.Caption := 'Status: ViGEmBus terdeteksi.';
    ViGEmStatusLabel.Font.Color := clGreen;
    ViGEmInstalledCheckBox.Checked := True;
  end
  else
  begin
    ViGEmStatusLabel.Caption := 'Status: ViGEmBus belum terdeteksi.';
    ViGEmStatusLabel.Font.Color := clRed;
  end;
end;

procedure RecheckViGEmStatus(Sender: TObject);
begin
  UpdateViGEmStatus;
end;

procedure InitializeWizard;
var
  InfoLabel: TNewStaticText;
begin
  ViGEmPage := CreateCustomPage(
    wpSelectTasks,
    'Prasyarat Driver',
    'ViGEmBus wajib terpasang sebelum aplikasi dijalankan.'
  );

  InfoLabel := TNewStaticText.Create(ViGEmPage);
  InfoLabel.Parent := ViGEmPage.Surface;
  InfoLabel.Left := ScaleX(0);
  InfoLabel.Top := ScaleY(0);
  InfoLabel.Width := ViGEmPage.SurfaceWidth;
  InfoLabel.Height := ScaleY(38);
  InfoLabel.AutoSize := False;
  InfoLabel.WordWrap := True;
  InfoLabel.Caption := 'Jika belum terpasang, klik ViGEmBus untuk membuka halaman rilis resmi.';

  ViGEmInstalledCheckBox := TNewCheckBox.Create(ViGEmPage);
  ViGEmInstalledCheckBox.Parent := ViGEmPage.Surface;
  ViGEmInstalledCheckBox.Left := ScaleX(0);
  ViGEmInstalledCheckBox.Top := InfoLabel.Top + InfoLabel.Height + ScaleY(8);
  ViGEmInstalledCheckBox.Caption := 'Sudah menginstall';
  ViGEmInstalledCheckBox.Checked := False;

  ViGEmLinkLabel := TNewStaticText.Create(ViGEmPage);
  ViGEmLinkLabel.Parent := ViGEmPage.Surface;
  ViGEmLinkLabel.Left := ViGEmInstalledCheckBox.Left + ViGEmInstalledCheckBox.Width + ScaleX(4);
  ViGEmLinkLabel.Top := ViGEmInstalledCheckBox.Top + ScaleY(2);
  ViGEmLinkLabel.Caption := 'ViGEmBus';
  ViGEmLinkLabel.Font.Color := clBlue;
  ViGEmLinkLabel.Font.Style := [fsUnderline];
  ViGEmLinkLabel.Cursor := crHand;
  ViGEmLinkLabel.OnClick := @OpenViGEmBusLink;

  ViGEmAutoInstallCheckBox := TNewCheckBox.Create(ViGEmPage);
  ViGEmAutoInstallCheckBox.Parent := ViGEmPage.Surface;
  ViGEmAutoInstallCheckBox.Left := ScaleX(0);
  ViGEmAutoInstallCheckBox.Top := ViGEmInstalledCheckBox.Top + ScaleY(28);
  ViGEmAutoInstallCheckBox.Caption := 'Install otomatis ViGEmBus jika belum terdeteksi';
  ViGEmAutoInstallCheckBox.Checked := True;

  ViGEmRecheckButton := TNewButton.Create(ViGEmPage);
  ViGEmRecheckButton.Parent := ViGEmPage.Surface;
  ViGEmRecheckButton.Caption := 'Cek Ulang';
  ViGEmRecheckButton.Left := ScaleX(0);
  ViGEmRecheckButton.Top := ViGEmAutoInstallCheckBox.Top + ScaleY(28);
  ViGEmRecheckButton.Width := ScaleX(90);
  ViGEmRecheckButton.Height := ScaleY(24);
  ViGEmRecheckButton.OnClick := @RecheckViGEmStatus;

  ViGEmStatusLabel := TNewStaticText.Create(ViGEmPage);
  ViGEmStatusLabel.Parent := ViGEmPage.Surface;
  ViGEmStatusLabel.Left := ViGEmRecheckButton.Left + ViGEmRecheckButton.Width + ScaleX(10);
  ViGEmStatusLabel.Top := ViGEmRecheckButton.Top + ScaleY(5);
  ViGEmStatusLabel.AutoSize := True;
  ViGEmStatusLabel.Caption := '';

  UpdateViGEmStatus;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = ViGEmPage.ID then
  begin
    UpdateViGEmStatus;

    if IsViGEmBusInstalled then
    begin
      ViGEmInstalledCheckBox.Checked := True;
    end
    else
    begin
      if (not ViGEmInstalledCheckBox.Checked) and (not ViGEmAutoInstallCheckBox.Checked) then
      begin
        MsgBox(
          'ViGEmBus belum terdeteksi.'#13#10 +
          'Centang install otomatis, atau install manual lalu centang konfirmasi.',
          mbError,
          MB_OK
        );
        Result := False;
      end;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  InstallerPath: string;
  RanOk: Boolean;
begin
  Result := '';

  if IsViGEmBusInstalled then
    Exit;

  if not ViGEmAutoInstallCheckBox.Checked then
  begin
    Result := 'ViGEmBus belum terdeteksi. Aktifkan install otomatis atau install manual terlebih dahulu.';
    Exit;
  end;

  ExtractTemporaryFile('{#ViGEmInstallerFile}');
  InstallerPath := GetViGEmInstallerPath;

  if not FileExists(InstallerPath) then
  begin
    Result := 'Installer ViGEmBus tidak ditemukan: ' + InstallerPath;
    Exit;
  end;

  RanOk := Exec(
    InstallerPath,
    '/quiet /norestart',
    '',
    SW_SHOWNORMAL,
    ewWaitUntilTerminated,
    ResultCode
  );

  if not RanOk then
  begin
    Result := 'Gagal menjalankan installer ViGEmBus.';
    Exit;
  end;

  if (ResultCode = 3010) or (ResultCode = 1641) then
    NeedsRestart := True;

  if not IsViGEmBusInstalled then
  begin
    Result :=
      'ViGEmBus masih belum terdeteksi setelah proses install otomatis.'#13#10 +
      'Silakan install manual dari halaman rilis, lalu jalankan setup ulang.';
  end;
end;

function InitializeUninstall: Boolean;
var
  ConfirmForm: TSetupForm;
  InfoLabel: TNewStaticText;
  RemoveDataCheckBox: TNewCheckBox;
  OkButton: TNewButton;
  CancelButton: TNewButton;
begin
  DeleteUserDataOnUninstall := False;
  Result := True;

  ConfirmForm := CreateCustomForm;
  try
    ConfirmForm.Caption := 'Konfirmasi Uninstall';
    ConfirmForm.ClientWidth := ScaleX(460);
    ConfirmForm.ClientHeight := ScaleY(190);

    InfoLabel := TNewStaticText.Create(ConfirmForm);
    InfoLabel.Parent := ConfirmForm;
    InfoLabel.Left := ScaleX(10);
    InfoLabel.Top := ScaleY(10);
    InfoLabel.Width := ConfirmForm.ClientWidth - ScaleX(20);
    InfoLabel.Height := ScaleY(58);
    InfoLabel.AutoSize := False;
    InfoLabel.WordWrap := True;
    InfoLabel.Caption :=
      'Aplikasi akan dihapus dari komputer ini.'#13#10 +
      'Centang opsi di bawah jika ingin ikut menghapus data pengguna di AppData.';

    RemoveDataCheckBox := TNewCheckBox.Create(ConfirmForm);
    RemoveDataCheckBox.Parent := ConfirmForm;
    RemoveDataCheckBox.Left := ScaleX(10);
    RemoveDataCheckBox.Top := InfoLabel.Top + InfoLabel.Height + ScaleY(8);
    RemoveDataCheckBox.Width := ConfirmForm.ClientWidth - ScaleX(20);
    RemoveDataCheckBox.Caption := 'Ikut menghapus data pengguna';
    RemoveDataCheckBox.Checked := False;

    OkButton := TNewButton.Create(ConfirmForm);
    OkButton.Parent := ConfirmForm;
    OkButton.Caption := SetupMessage(msgButtonOK);
    OkButton.ModalResult := mrOk;
    OkButton.Default := True;
    OkButton.SetBounds(
      ConfirmForm.ClientWidth - ScaleX(182),
      ConfirmForm.ClientHeight - ScaleY(34),
      ScaleX(80),
      ScaleY(24)
    );

    CancelButton := TNewButton.Create(ConfirmForm);
    CancelButton.Parent := ConfirmForm;
    CancelButton.Caption := SetupMessage(msgButtonCancel);
    CancelButton.ModalResult := mrCancel;
    CancelButton.Cancel := True;
    CancelButton.SetBounds(
      ConfirmForm.ClientWidth - ScaleX(92),
      ConfirmForm.ClientHeight - ScaleY(34),
      ScaleX(80),
      ScaleY(24)
    );

    if ConfirmForm.ShowModal = mrOk then
      DeleteUserDataOnUninstall := RemoveDataCheckBox.Checked
    else
      Result := False;
  finally
    ConfirmForm.Free;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: string;
begin
  if (CurUninstallStep = usPostUninstall) and DeleteUserDataOnUninstall then
  begin
    DataDir := ExpandConstant('{#MyAppDataDir}');
    if DirExists(DataDir) then
      DelTree(DataDir, True, True, True);
  end;
end;