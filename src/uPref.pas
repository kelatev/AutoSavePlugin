// Created by x00man01d
// Copyright (c) 2015. All rights reserved.
unit uPref;

interface

uses
  Windows, SysUtils, Classes, Vcl.Controls, Vcl.Forms, Vcl.StdCtrls, PlugInIntf, Vcl.ExtCtrls,
  ShlObj, Vcl.Buttons;

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
  begin
    if ShowModal = mrOK then
    begin
      IDE_SetPrefAsString(PlugInID, '', 'path',
        PAnsiChar(AnsiString(edPath.Text)));
    end;
    Free;
  end;
//frmPref := nil;
end;

procedure TfrmPref.btnSelectPathClick(Sender: TObject);
begin
  edPath.Text := GetFolder;
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
  FillChar(BrowseInfo, sizeof(TBrowseInfo), #0);
  BrowseInfo.hwndOwner := Self.Handle;
  BrowseInfo.pszDisplayName := @DisplayName;
  BrowseInfo.lpszTitle := PChar('Choose folder');
  BrowseInfo.ulFlags := BIF_RETURNONLYFSDIRS;
  lpItemID := SHBrowseForFolder(BrowseInfo);
  if lpItemID <> nil then
  begin
    SHGetPathFromIDList(lpItemID, TempPath);
    result := TempPath;
    GlobalFreePtr(lpItemID);
  end;

end;

end.
