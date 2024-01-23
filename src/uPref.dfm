object frmPref: TfrmPref
  Left = 527
  Top = 470
  BorderStyle = bsDialog
  Caption = 'Autosave Preferences'
  ClientHeight = 82
  ClientWidth = 409
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -11
  Font.Name = 'MS Sans Serif'
  Font.Style = []
  Position = poScreenCenter
  OnCreate = FormCreate
  TextHeight = 13
  object btnSelectPath: TSpeedButton
    Left = 372
    Top = 2
    Width = 24
    Height = 24
    Font.Charset = DEFAULT_CHARSET
    Font.Color = clWindowText
    Font.Height = -13
    Font.Name = 'MS Sans Serif'
    Font.Style = []
    Glyph.Data = {
      6E040000424D6E040000000000003600000028000000120000000F0000000100
      20000000000038040000120B0000120B00000000000000000000008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      0100008001000000000000000000000000000000000000000000000000000000
      0000000000000000000000000000000000000080010000800100008001000080
      0100008001000080010000800100000000000000000000808000008080000080
      8000008080000080800000808000008080000080800000808000000000000080
      010000800100008001000080010000800100008001000000000000FFFF000000
      0000008080000080800000808000008080000080800000808000008080000080
      8000008080000000000000800100008001000080010000800100008001000000
      0000FFFFFF0000FFFF0000000000008080000080800000808000008080000080
      8000008080000080800000808000008080000000000000800100008001000080
      0100008001000000000000FFFF00FFFFFF0000FFFF0000000000008080000080
      8000008080000080800000808000008080000080800000808000008080000000
      000000800100008001000080010000000000FFFFFF0000FFFF00FFFFFF0000FF
      FF00000000000000000000000000000000000000000000000000000000000000
      000000000000000000000000000000800100008001000000000000FFFF00FFFF
      FF0000FFFF00FFFFFF0000FFFF00FFFFFF0000FFFF00FFFFFF0000FFFF000000
      0000008001000080010000800100008001000080010000800100008001000000
      0000FFFFFF0000FFFF00FFFFFF0000FFFF00FFFFFF0000FFFF00FFFFFF0000FF
      FF00FFFFFF000000000000800100008001000080010000800100008001000080
      0100008001000000000000FFFF00FFFFFF0000FFFF0000000000000000000000
      0000000000000000000000000000000000000080010000800100008001000080
      0100008001000080010000800100008001000000000000000000000000000080
      0100008001000080010000800100008001000080010000800100008001000000
      0000000000000000000000800100008001000080010000800100008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      0100008001000080010000000000000000000080010000800100008001000080
      0100008001000080010000800100008001000080010000800100008001000000
      0000008001000080010000800100000000000080010000000000008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      0100008001000080010000000000000000000000000000800100008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      0100008001000080010000800100008001000080010000800100008001000080
      010000800100008001000080010000800100}
    ParentFont = False
    OnClick = btnSelectPathClick
  end
  object edPath: TEdit
    Left = 5
    Top = 2
    Width = 361
    Height = 21
    TabOrder = 0
    Text = 'Path'
  end
  object btnOK: TButton
    Left = 144
    Top = 35
    Width = 70
    Height = 25
    Caption = 'Save'
    Default = True
    ModalResult = 1
    TabOrder = 2
  end
  object btnCancel: TButton
    Left = 224
    Top = 35
    Width = 70
    Height = 25
    Cancel = True
    Caption = 'Cancel'
    ModalResult = 2
    TabOrder = 1
  end
end
