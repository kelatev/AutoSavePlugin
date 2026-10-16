// Created by x00man01d
// Copyright (c) 2015. All rights reserved.
unit uPref;

interface

uses
  Windows, SysUtils, Classes, Vcl.Controls, Vcl.Forms, Vcl.StdCtrls, PlugInIntf, Vcl.ExtCtrls,
  ShlObj, ActiveX, Vcl.Buttons;

type
  TfrmPref = class(TForm)
    edPath: TEdit;
    btnSelectPath: TSpeedButton;
    btnOK: TButton;
    btnCancel: TButton;
    procedure FormCreate(Sender: TObject);
    procedure btnSelectPathClick(Sender: TObject);
  private
    function GetFolder: string;
  public
    { Public declarations }
  end;


procedure DoPreferences;

implementation

{$R *.DFM}

procedure DoPreferences;
begin
  with TfrmPref.Create(Application) do
    try
      if ShowModal = mrOK then
      begin
        IDE_SetPrefAsString(PlugInID, '', 'path',
          PAnsiChar(AnsiString(Trim(edPath.Text))));
      end;
    finally
      Free;
    end;
end;

procedure TfrmPref.btnSelectPathClick(Sender: TObject);
var
  Folder: string;
begin
  Folder := GetFolder;
  if Folder <> '' then
    edPath.Text := Folder;
end;

procedure TfrmPref.FormCreate(Sender: TObject);
begin
  edPath.Text := String(IDE_GetPrefAsString(PlugInID, '', 'path', 'C:\AutoSavePlugin'));
end;

function TfrmPref.GetFolder: string;
var
  lpItemID: PItemIDList;
  BrowseInfo: TBrowseInfo;
  DisplayName: array [0 .. MAX_PATH] of char;
  TempPath: array [0 .. MAX_PATH] of char;
begin
  Result := '';
  FillChar(BrowseInfo, sizeof(TBrowseInfo), #0);
  BrowseInfo.hwndOwner := Self.Handle;
  BrowseInfo.pszDisplayName := @DisplayName;
  BrowseInfo.lpszTitle := PChar('Choose folder');
  BrowseInfo.ulFlags := BIF_RETURNONLYFSDIRS;
  lpItemID := SHBrowseForFolder(BrowseInfo);
  if lpItemID <> nil then
  begin
    if SHGetPathFromIDList(lpItemID, TempPath) then
      Result := TempPath;
    CoTaskMemFree(lpItemID);
  end;
end;

end.
