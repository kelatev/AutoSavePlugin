// Created by x00man01d
// Copyright (c) 2015. All rights reserved.

unit uMain;

interface

uses
  Windows, Vcl.Forms, PlugInIntf, SysUtils, Vcl.Dialogs,
  System.UITypes, System.Classes;

const // Description of this Plug-In (as displayed in Plug-In configuration dialog)
  Desc: AnsiString = 'AutoSavePlugin';
  AboutText: AnsiString = 'AutoSavePlugin';

var
  FixTime: String;  // Timestamp shared by the Before and After copies of one execution
  SavePath: String; // Root folder resolved once per execution

procedure FileFixation(Before: Bool);
procedure TimeFixation();
procedure SaveFile(Before: Bool; Database: String; ObjectOwner: String;
                   ObjectType: String; ObjectName: String;
                   ObjectSource: String; FileExt: String);
function GetPrefPath(): String;

implementation

uses uPref;

// Plug-In identification, a unique identifier is received and
// the description is returned

function IdentifyPlugIn(ID: Integer): PAnsiChar; cdecl;
begin
  PlugInID := ID;
  Result := PAnsiChar(Desc);
end;

function BeforeExecuteWindow(WindowType: Integer): Bool; cdecl;
begin
  try
    TimeFixation();
    SavePath := GetPrefPath();
    FileFixation(True);
  except
    on E: Exception do
      IDE_DebugLog(PAnsiChar(AnsiString('AutoSavePlugin: ' + E.Message)));
  end;
  Result := True;
end;

procedure AfterExecuteWindow(WindowType, Result: Integer); cdecl;
begin
  if Result = 2 then
     FileFixation(False);
end;

// Replaces characters that are not allowed in file and folder names
// (e.g. "host:1521/service" connection strings)
function SafeName(const S: String): String;
const
  Bad = '\/:*?"<>|';
var
  i: Integer;
begin
  Result := Trim(S);
  for i := 1 to Length(Result) do
    if (Result[i] < ' ') or (Pos(Result[i], Bad) > 0) then
      Result[i] := '_';
  if Result = '' then
    Result := '_';
end;

// Database of the connection used by the current window
function GetWindowDatabase(): String;
var
  AUsername, APassword, ADatabase, AConnectAs: PAnsiChar;
begin
  AUsername := nil; APassword := nil; ADatabase := nil; AConnectAs := nil;
  if Assigned(IDE_GetWindowConnection) and Assigned(IDE_GetConnectionInfoEx) and
     IDE_GetConnectionInfoEx(IDE_GetWindowConnection(), AUsername, APassword,
                             ADatabase, AConnectAs) then
    Result := String(ADatabase)
  else
  begin
    IDE_GetConnectionInfo(AUsername, APassword, ADatabase);
    Result := String(ADatabase);
  end;
end;

procedure FileFixation(Before: Bool);
var
  AObjectType, AObjectOwner, AObjectName, ASubObject: PAnsiChar;

  SDatabase, SObjectOwner, SObjectType, SObjectName, FileExt: String;
  SObjectSource: String;
begin
  if SavePath = '' then
    Exit;

  try
    AObjectType := nil; AObjectOwner := nil; AObjectName := nil; ASubObject := nil;
    if not IDE_GetWindowObject(AObjectType, AObjectOwner, AObjectName, ASubObject) then
      Exit;

    SDatabase := GetWindowDatabase();
    SObjectOwner := String(AObjectOwner);
    SObjectType := String(AObjectType);
    SObjectName := String(AObjectName);
    if (SObjectType = 'PACKAGE') or (SObjectType = 'PACKAGE BODY') then
    begin
       FileExt := 'pck';
       SObjectType := 'PACKAGE';
       SObjectSource := TrimRight(String(IDE_GetObjectSource('PACKAGE', AObjectOwner, AObjectName)));
       SObjectSource := SObjectSource + #13#10 + '/' + #13#10;
       SObjectSource := SObjectSource + String(IDE_GetObjectSource('PACKAGE BODY', AObjectOwner, AObjectName));
    end
    else
    begin
       FileExt := 'sql';
       SObjectSource := String(IDE_GetObjectSource(AObjectType, AObjectOwner, AObjectName));
    end;

    if not (SObjectSource = '') and not (SObjectName = '') then
      SaveFile(Before, SDatabase, SObjectOwner,
               SObjectType, SObjectName, SObjectSource, FileExt);
  except
    on E: Exception do
      IDE_DebugLog(PAnsiChar(AnsiString('AutoSavePlugin: ' + E.Message)));
  end;
end;

procedure TimeFixation();
begin
  FixTime := FormatDateTime('yyyy"."mm"."dd hh"-"nn"-"ss"."zzz', Now);
end;

procedure SaveFile(Before: Bool; Database: String; ObjectOwner: String;
                   ObjectType: String; ObjectName: String;
                   ObjectSource: String; FileExt: String);
var
  ActionType: String;
  Path: String;
  Filename: String;
begin
  if Before then
    ActionType := 'Before'
  else
    ActionType := 'After';

  Path := ExcludeTrailingPathDelimiter(SavePath) + '\' +
          SafeName(Database) + '\' +
          SafeName(ObjectOwner) + '\' +
          SafeName(ObjectType) + '\' +
          SafeName(ObjectName) + '\' +
          FixTime + '\' +
          ActionType;
  if not ForceDirectories(Path) then
    raise Exception.Create('Cannot create folder ' + Path);

  Filename := Path + '\' + SafeName(ObjectName) + '.' + FileExt;

  with TStringList.Create do
    try
      Text := ObjectSource;
      SaveToFile(Filename);
    finally
      Free;
    end;
end;

function GetPrefPath(): String;
begin
  Result := Trim(String(IDE_GetPrefAsString(PlugInID, '', 'path', '')));
  if Result = '' then
  begin
    if MessageDlg('The path to save the files is not specified.' + #13#10 +
      'Specify now?', mtConfirmation, [mbYes, mbNo], 0) = mrYes
    then
    begin
      DoPreferences;
      Result := Trim(String(IDE_GetPrefAsString(PlugInID, '', 'path', '')));
    end;
  end;
end;

// OnActivate gets called after OnCreate. However, when OnActivate is called PL/SQL
// Developer and the Plug-In are fully initialized. This function is also called when the
// Plug-In is enabled in the configuration dialog. A good point to enable/disable menus.

procedure OnActivate; cdecl;
begin
  Application.Handle := IDE_GetAppHandle;
end;

// This will be called when PL/SQL Developer is about to close. If your PlugIn is not
// ready to close, you can show a message and return False.

function CanClose: Bool; cdecl;
begin
  Result := True;
end;

// This function allows you to take some action before a window is closed. You can
// influence the closing of the window with the following return values:
// 0 = Default behavior
// 1 = Ask the user for confirmation (like the contents was changed)
// 2 = Don't ask, allow to close without confirmation
// The Changed Boolean indicates the current status of the window.

function OnWindowClose(WindowType: Integer; Changed: Bool): Integer; cdecl;
begin
  Result := 0;
end;

// This function allows you to display an about dialog. You can decide to display a
// dialog yourself (in which case you should return an empty text) or just return the
// about text.

function About: PAnsiChar; cdecl;
begin
  Result := PAnsiChar(AboutText);
end;

// If the Plug-In has a configure dialog you could use this function to activate it. This will
// allow a user to configure your Plug-In using the configure button in the Plug-In
// configuration dialog.

procedure Configure; cdecl;
begin
  DoPreferences;
end;

// Exported functions
exports IdentifyPlugIn, RegisterCallback, OnActivate, CanClose, OnWindowClose,
  About, Configure, BeforeExecuteWindow, AfterExecuteWindow;

end.
