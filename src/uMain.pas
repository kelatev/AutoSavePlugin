// Created by x00man01d
// Copyright (c) 2015. All rights reserved.

unit uMain;

interface

uses
  Windows, Vcl.Forms, PlugInIntf, SysUtils, Vcl.Dialogs,
  System.UITypes, System.Classes, DateUtils;

const // Description of this Plug-In (as displayed in Plug-In configuration dialog)
  Desc: AnsiString = 'AutoSavePlugin';

var
  Time: String;

procedure FileFixation(Before: Bool);
procedure TimeFixation();
procedure SaveFile(Before: Bool; Database: String; ObjectOwner: String;
                   ObjectType: String; ObjectName: String;
                   ObjectSource: String; FileExt: String);
function GetPrefPath(): AnsiString;

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
  TimeFixation();
  FileFixation(True);
  Result := True;
end;

procedure AfterExecuteWindow(WindowType, Result: Integer); cdecl;
begin
  if Result = 2 then
     FileFixation(False);
end;

procedure FileFixation(Before: Bool);
var
  AUsername, APassword, ADatabase: PAnsiChar;
  AObjectType, AObjectOwner, AObjectName, ASubObject: PAnsiChar;

  SDatabase, SObjectOwner, SObjectType, SObjectName, FileExt: String;
  SObjectSource: String;
begin
  IDE_DebugLog('FileFixation');

  try
    IDE_GetConnectionInfo(AUsername, APassword, ADatabase);
    SDatabase := String(ADatabase);
    IDE_GetWindowObject(AObjectType, AObjectOwner, AObjectName, ASubObject);
    SObjectOwner := String(AObjectOwner);
    SObjectType := String(AObjectType);
    SObjectName := String(AObjectName);
    if (SObjectType = 'PACKAGE') or (SObjectType = 'PACKAGE BODY') then
    begin
       FileExt := 'pck';
       SObjectType := 'PACKAGE';
       SObjectSource := String(IDE_GetObjectSource('PACKAGE', AObjectOwner, AObjectName));
       SObjectSource := SObjectSource + '/' + AnsiString(#13#10);
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

  end;
end;

procedure TimeFixation();
var
  today : TDateTime;
  lYear, lMonth, lDay: Word;
  lHour, lMin, lSec, lMilli: Word;
begin
  today := Now;
  DecodeDateTime(today, lYear, lMonth, lDay,
                 lHour, lMin, lSec, lMilli);

  Time := IntToStr(lYear);
  if lMonth < 10 then
    Time := Time + '.0' + IntToStr(lMonth)
  else
    Time := Time + '.' + IntToStr(lMonth);
  if lDay < 10 then
    Time := Time + '.0' + IntToStr(lDay)
  else
    Time := Time + '.' + IntToStr(lDay);
  if lHour < 10 then
    Time := Time + ' 0' + IntToStr(lHour)
  else
    Time := Time + ' ' + IntToStr(lHour);
  if lMin < 10 then
    Time := Time + '-0' + IntToStr(lMin)
  else
    Time := Time + '-' + IntToStr(lMin);
  if lSec < 10 then
    Time := Time + '-0' + IntToStr(lSec)
  else
    Time := Time + '-' + IntToStr(lSec);
  Time := Time + '.' + IntToStr(lMilli);
end;

procedure SaveFile(Before: Bool; Database: String; ObjectOwner: String;
                   ObjectType: String; ObjectName: String;
                   ObjectSource: String; FileExt: String);
var
  AtionType: String;
  Path: String;
  Filename: String;
begin
  if Before = True then
    begin
       AtionType := 'Before';
    end
  else
    begin
      AtionType := 'After';
    end;
  Path := String(GetPrefPath());
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + Database;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + ObjectOwner;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + ObjectType;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + ObjectName;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + Time;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Path := Path + '\' + AtionType;
  if not DirectoryExists(Path) then
    ForceDirectories(Path);

  Filename := Path + '\' + ObjectName + '.' + FileExt;

  with TStringList.Create do
    try
      Add(ObjectSource);
      SaveToFile(Filename);
    finally
      Free;
    end;

  //try
  //  AssignFile(myFile, Filename);
  //  Write(myFile, ObjectSource);
  //  CloseFile(myFile);
  //except

  //end;
end;

function GetPrefPath(): AnsiString;
var
  APath: AnsiString;
begin
  Result := '';
  APath := IDE_GetPrefAsString(PlugInID, '', 'path', '');
  if APath = '' then
  begin
    if MessageDlg('The path to save the files is not specified.' + #10#13 +
      'Specify now?', mtConfirmation, [mbYes, mbNo, mbCancel], 0) = mrYes
    then
    begin
      DoPreferences;
      GetPrefPath();
    end;
  end;
  Result := APath;
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
// 2 = Don’t ask, allow to close without confirmation
// The Changed Boolean indicates the current status of the window.

function OnWindowClose(WindowType: Integer; Changed: Bool): Integer; cdecl;
begin
  Result := 0;
end;

// This function allows you to display an about dialog. You can decide to display a
// dialog yourself (in which case you should return an empty text) or just return the
// about text.

function About: PChar; cdecl;
begin
  Result := 'AutoSavePlugin - x64';
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
