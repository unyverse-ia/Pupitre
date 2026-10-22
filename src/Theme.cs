using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Pupitre
{
    /// <summary>Thème sombre « verre » : pinceaux, icônes vectorielles et styles des contrôles, chargés depuis du XAML à l'exécution.</summary>
    internal static class Theme
    {
        public static readonly FontFamily Font = new FontFamily("Segoe UI Variable Text, Segoe UI");

        const string Xaml = @"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <FontFamily x:Key='UiFont'>Segoe UI Variable Text, Segoe UI</FontFamily>
  <SolidColorBrush x:Key='Text' Color='#F2FFFFFF'/>
  <SolidColorBrush x:Key='TextMuted' Color='#A6FFFFFF'/>
  <SolidColorBrush x:Key='TextFaint' Color='#66FFFFFF'/>
  <SolidColorBrush x:Key='Hover' Color='#1EFFFFFF'/>
  <SolidColorBrush x:Key='Pressed' Color='#36FFFFFF'/>
  <SolidColorBrush x:Key='Selected' Color='#405B8DEF'/>
  <SolidColorBrush x:Key='FieldBg' Color='#30000000'/>
  <SolidColorBrush x:Key='FieldBorder' Color='#28FFFFFF'/>
  <SolidColorBrush x:Key='Line' Color='#22FFFFFF'/>
  <SolidColorBrush x:Key='Accent' Color='#5B8DEF'/>
  <SolidColorBrush x:Key='AccentHover' Color='#7AA3F5'/>
  <SolidColorBrush x:Key='Danger' Color='#F2554B'/>
  <SolidColorBrush x:Key='Warn' Color='#FFA64D'/>
  <SolidColorBrush x:Key='Ok' Color='#3FBF7F'/>
  <SolidColorBrush x:Key='MenuBg' Color='#F6202024'/>
  <SolidColorBrush x:Key='MenuBorder' Color='#36FFFFFF'/>
  <SolidColorBrush x:Key='SBg' Color='#FF1E1E1E'/>
  <SolidColorBrush x:Key='SLine' Color='#FF3F3F46'/>
  <SolidColorBrush x:Key='SText' Color='#FFE6E6E6'/>
  <SolidColorBrush x:Key='SMuted' Color='#FF9D9DA3'/>
  <SolidColorBrush x:Key='SField' Color='#FF2D2D30'/>
  <SolidColorBrush x:Key='SHover' Color='#FF3E3E42'/>
  <SolidColorBrush x:Key='SPressed' Color='#FF505055'/>
  <SolidColorBrush x:Key='SError' Color='#FFF48771'/>

  <Geometry x:Key='IcoPlus'>M5 12h14 M12 5v14</Geometry>
  <Geometry x:Key='IcoCheck'>M20 6 9 17l-5-5</Geometry>
  <Geometry x:Key='IcoTrash'>M3 6h18 M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6 M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2 M10 11v6 M14 11v6</Geometry>
  <Geometry x:Key='IcoPencil'>M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z M15 5l4 4</Geometry>
  <Geometry x:Key='IcoCalendar'>M8 2v4 M16 2v4 M3 10h18 M5 4h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z</Geometry>
  <Geometry x:Key='IcoFlag'>M4 15s1-1 4-1 5 2 8 2 4-1 4-1V3s-1 1-4 1-5-2-8-2-4 1-4 1z M4 22v-7</Geometry>
  <Geometry x:Key='IcoTag'>M12.586 2.586A2 2 0 0 0 11.172 2H4a2 2 0 0 0-2 2v7.172a2 2 0 0 0 .586 1.414l8.704 8.704a2.426 2.426 0 0 0 3.42 0l6.58-6.58a2.426 2.426 0 0 0 0-3.42z M7.5 7.5h.01</Geometry>
  <Geometry x:Key='IcoFolder'>M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z</Geometry>
  <Geometry x:Key='IcoFolderOpen'>m6 14 1.5-2.9A2 2 0 0 1 9.24 10H20a2 2 0 0 1 1.94 2.5l-1.54 6a2 2 0 0 1-1.95 1.5H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h3.9a2 2 0 0 1 1.69.9l.81 1.2a2 2 0 0 0 1.67.9H18a2 2 0 0 1 2 2v2</Geometry>
  <Geometry x:Key='IcoSettings'>M20 7h-9 M14 17H5 M17 17m-3 0a3 3 0 1 0 6 0a3 3 0 1 0-6 0 M7 7m-3 0a3 3 0 1 0 6 0a3 3 0 1 0-6 0</Geometry>
  <Geometry x:Key='IcoChevronDown'>m6 9 6 6 6-6</Geometry>
  <Geometry x:Key='IcoChevronUp'>m18 15-6-6-6 6</Geometry>
  <Geometry x:Key='IcoChevronRight'>m9 18 6-6-6-6</Geometry>
  <Geometry x:Key='IcoSearch'>M3 11a8 8 0 1 0 16 0a8 8 0 1 0-16 0 M21 21l-4.3-4.3</Geometry>
  <Geometry x:Key='IcoDots'>M12 12h.01 M19 12h.01 M5 12h.01</Geometry>
  <Geometry x:Key='IcoClose'>M18 6 6 18 M6 6l12 12</Geometry>
  <Geometry x:Key='IcoGrip'>M9 5h.01 M9 12h.01 M9 19h.01 M15 5h.01 M15 12h.01 M15 19h.01</Geometry>
  <Geometry x:Key='IcoComment'>M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z</Geometry>
  <Geometry x:Key='IcoNotes'>M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z M14 2v4a2 2 0 0 0 2 2h4 M16 13H8 M16 17H8</Geometry>
  <Geometry x:Key='IcoRefresh'>M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8 M21 3v5h-5 M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16 M8 16H3v5</Geometry>
  <Geometry x:Key='IcoExternal'>M15 3h6v6 M10 14 21 3 M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6</Geometry>
  <Geometry x:Key='IcoLock'>M5 11h14a2 2 0 0 1 2 2v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2z M7 11V7a5 5 0 0 1 10 0v4</Geometry>
  <Geometry x:Key='IcoInbox'>M22 12h-6l-2 3h-4l-2-3H2 M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z</Geometry>
  <Geometry x:Key='IcoGridLayout'>M3 3h7v7H3z M14 3h7v7h-7z M14 14h7v7h-7z M3 14h7v7H3z</Geometry>
  <Geometry x:Key='IcoCopy'>M20 9h-9a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2-2v-9a2 2 0 0 0-2-2z M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1</Geometry>
  <Geometry x:Key='IcoSort'>M11 5h10 M11 9h7 M11 13h4 M3 17l3 3 3-3 M6 18V4</Geometry>
  <Geometry x:Key='IcoEye'>M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z M12 12m-3 0a3 3 0 1 0 6 0a3 3 0 1 0-6 0</Geometry>
  <Geometry x:Key='IcoInfo'>M12 12m-10 0a10 10 0 1 0 20 0a10 10 0 1 0-20 0 M12 16v-4 M12 8h.01</Geometry>
  <Geometry x:Key='IcoList'>M8 6h13 M8 12h13 M8 18h13 M3 6h.01 M3 12h.01 M3 18h.01</Geometry>
  <Geometry x:Key='IcoPalette'>M12 22a10 10 0 1 1 0-20 10 10 0 0 1 10 10c0 1.7-1.3 3-3 3h-2a2 2 0 0 0-1.5 3.3c.3.4.5.9.5 1.4 0 1.3-1 2.3-2.3 2.3Z M7.5 10.5h.01 M12 7.5h.01 M16.5 10.5h.01</Geometry>
  <Geometry x:Key='IcoCheckSquare'>M9 11l3 3L22 4 M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11</Geometry>

  <Style x:Key='Ico' TargetType='{x:Type Path}'>
    <Setter Property='Width' Value='24'/>
    <Setter Property='Height' Value='24'/>
    <Setter Property='Stretch' Value='None'/>
    <Setter Property='StrokeThickness' Value='2'/>
    <Setter Property='StrokeLineJoin' Value='Round'/>
    <Setter Property='StrokeStartLineCap' Value='Round'/>
    <Setter Property='StrokeEndLineCap' Value='Round'/>
    <Setter Property='Stroke' Value='{Binding Path=(TextElement.Foreground), RelativeSource={RelativeSource Self}}'/>
  </Style>

  <Style x:Key='IconBtn' TargetType='{x:Type Button}'>
    <Setter Property='Width' Value='26'/>
    <Setter Property='Height' Value='26'/>
    <Setter Property='Foreground' Value='{StaticResource TextMuted}'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' Background='{TemplateBinding Background}' CornerRadius='6'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Hover}'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Pressed}'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter Property='Opacity' Value='0.3'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='IconBtnSmall' TargetType='{x:Type Button}' BasedOn='{StaticResource IconBtn}'>
    <Setter Property='Width' Value='22'/>
    <Setter Property='Height' Value='22'/>
  </Style>
  <Style x:Key='GhostTextBox' TargetType='{x:Type TextBox}'>
    <Setter Property='Foreground' Value='{StaticResource Text}'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='CaretBrush' Value='White'/>
    <Setter Property='SelectionBrush' Value='{StaticResource Accent}'/>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Padding' Value='2,2,2,2'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type TextBox}'>
          <Grid>
            <ScrollViewer x:Name='PART_ContentHost' VerticalAlignment='{TemplateBinding VerticalContentAlignment}' Padding='{TemplateBinding Padding}'/>
            <TextBlock x:Name='Hint' Text='{TemplateBinding Tag}' Foreground='{StaticResource TextFaint}' Margin='4,0,0,0' VerticalAlignment='{TemplateBinding VerticalContentAlignment}' IsHitTestVisible='False' Visibility='Collapsed' FontSize='{TemplateBinding FontSize}'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='Text' Value=''>
              <Setter TargetName='Hint' Property='Visibility' Value='Visible'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='FieldTextBox' TargetType='{x:Type TextBox}'>
    <Setter Property='Foreground' Value='{StaticResource Text}'/>
    <Setter Property='Background' Value='{StaticResource FieldBg}'/>
    <Setter Property='BorderBrush' Value='{StaticResource FieldBorder}'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='CaretBrush' Value='White'/>
    <Setter Property='SelectionBrush' Value='{StaticResource Accent}'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='Padding' Value='7,4,7,4'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type TextBox}'>
          <Border x:Name='Bd' CornerRadius='6' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>
            <Grid>
              <ScrollViewer x:Name='PART_ContentHost' VerticalAlignment='Center' Padding='{TemplateBinding Padding}'/>
              <TextBlock x:Name='Hint' Text='{TemplateBinding Tag}' Foreground='{StaticResource TextFaint}' Margin='9,0,0,0' VerticalAlignment='Center' IsHitTestVisible='False' Visibility='Collapsed' FontSize='{TemplateBinding FontSize}'/>
            </Grid>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='Text' Value=''>
              <Setter TargetName='Hint' Property='Visibility' Value='Visible'/>
            </Trigger>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Accent}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Chip' TargetType='{x:Type RadioButton}'>
    <Setter Property='Foreground' Value='{StaticResource TextMuted}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='FontSize' Value='11'/>
    <Setter Property='Margin' Value='0,0,4,0'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type RadioButton}'>
          <Border x:Name='Bd' CornerRadius='10' Background='Transparent' Padding='9,2,9,3'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Hover}'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Pressed}'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='ChipBtn' TargetType='{x:Type Button}'>
    <Setter Property='Foreground' Value='{StaticResource TextMuted}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='FontSize' Value='11'/>
    <Setter Property='Margin' Value='0,0,4,0'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' CornerRadius='10' Background='{StaticResource Hover}' Padding='9,2,9,3'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Pressed}'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='ChipColored' TargetType='{x:Type Button}'>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='FontSize' Value='11'/>
    <Setter Property='Background' Value='{StaticResource Hover}'/>
    <Setter Property='Foreground' Value='{StaticResource TextMuted}'/>
    <Setter Property='Margin' Value='4,0,0,0'/>
    <Setter Property='VerticalAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' CornerRadius='5' Background='{TemplateBinding Background}' Padding='7,1,7,2'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Opacity' Value='0.75'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='DarkSlider' TargetType='{x:Type Slider}'>
    <Setter Property='Height' Value='22'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Slider}'>
          <Grid Background='Transparent'>
            <Border Height='4' CornerRadius='2' Background='#40FFFFFF' VerticalAlignment='Center' Margin='7,0,7,0'/>
            <Track x:Name='PART_Track' VerticalAlignment='Center'>
              <Track.DecreaseRepeatButton>
                <RepeatButton Command='Slider.DecreaseLarge' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='{x:Type RepeatButton}'>
                      <Border Height='4' CornerRadius='2' Background='{StaticResource Accent}' Margin='7,0,0,0'/>
                    </ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.DecreaseRepeatButton>
              <Track.IncreaseRepeatButton>
                <RepeatButton Command='Slider.IncreaseLarge' Focusable='False'>
                  <RepeatButton.Template>
                    <ControlTemplate TargetType='{x:Type RepeatButton}'>
                      <Border Background='Transparent' Height='22'/>
                    </ControlTemplate>
                  </RepeatButton.Template>
                </RepeatButton>
              </Track.IncreaseRepeatButton>
              <Track.Thumb>
                <Thumb Width='14' Height='14' Focusable='False' Cursor='Hand'>
                  <Thumb.Template>
                    <ControlTemplate TargetType='{x:Type Thumb}'>
                      <Ellipse Fill='White' Stroke='#60000000' StrokeThickness='1'/>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='AccentBtn' TargetType='{x:Type Button}'>
    <Setter Property='Foreground' Value='White'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='FontWeight' Value='SemiBold'/>
    <Setter Property='Background' Value='{StaticResource Accent}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' CornerRadius='9' Background='{TemplateBinding Background}' Padding='12,7,14,8'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Opacity' Value='0.86'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='Bd' Property='Opacity' Value='0.7'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='LinkBtn' TargetType='{x:Type Button}'>
    <Setter Property='Foreground' Value='{StaticResource TextFaint}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='FontSize' Value='11'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <TextBlock x:Name='T' Text='{TemplateBinding Content}' Foreground='{TemplateBinding Foreground}'/>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter Property='Foreground' Value='{StaticResource Text}'/>
              <Setter TargetName='T' Property='TextDecorations' Value='Underline'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='CheckCircle' TargetType='{x:Type ToggleButton}'>
    <Setter Property='Width' Value='22'/>
    <Setter Property='Height' Value='22'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Background' Value='{StaticResource Accent}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ToggleButton}'>
          <Grid Background='Transparent'>
           <Grid Width='22' Height='22' HorizontalAlignment='Center' VerticalAlignment='Center' UseLayoutRounding='False' SnapsToDevicePixels='False'>
            <Ellipse x:Name='Ring' Width='18' Height='18' Stroke='{StaticResource TextFaint}' StrokeThickness='1.6' Fill='Transparent'/>
            <Ellipse x:Name='Fill' Width='18' Height='18' Fill='{TemplateBinding Background}' Opacity='0'/>
            <Path x:Name='Mark' Data='M6.8 11.3 9.7 14.1 15.4 8.3' Stroke='White' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Stretch='None' Width='22' Height='22' Opacity='0'/>
           </Grid>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Ring' Property='Stroke' Value='{StaticResource TextMuted}'/>
              <Setter TargetName='Mark' Property='Opacity' Value='0.45'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Fill' Property='Opacity' Value='1'/>
              <Setter TargetName='Ring' Property='Opacity' Value='0'/>
              <Setter TargetName='Mark' Property='Opacity' Value='1'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='ScrollPage' TargetType='{x:Type RepeatButton}'>
    <Setter Property='IsTabStop' Value='False'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type RepeatButton}'>
          <Rectangle Fill='Transparent'/>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='ScrollThumb' TargetType='{x:Type Thumb}'>
    <Setter Property='IsTabStop' Value='False'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Thumb}'>
          <Border x:Name='Bd' CornerRadius='3' Background='#46FFFFFF' Margin='1'/>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='#80FFFFFF'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType='{x:Type ScrollBar}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Width' Value='9'/>
    <Setter Property='MinWidth' Value='9'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ScrollBar}'>
          <Grid Background='Transparent'>
            <Track x:Name='PART_Track' IsDirectionReversed='True'>
              <Track.DecreaseRepeatButton>
                <RepeatButton Style='{StaticResource ScrollPage}' Command='ScrollBar.PageUpCommand'/>
              </Track.DecreaseRepeatButton>
              <Track.IncreaseRepeatButton>
                <RepeatButton Style='{StaticResource ScrollPage}' Command='ScrollBar.PageDownCommand'/>
              </Track.IncreaseRepeatButton>
              <Track.Thumb>
                <Thumb Style='{StaticResource ScrollThumb}'/>
              </Track.Thumb>
            </Track>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='Orientation' Value='Horizontal'>
              <Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='False'/>
              <Setter Property='Width' Value='Auto'/>
              <Setter Property='MinWidth' Value='0'/>
              <Setter Property='Height' Value='7'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='{x:Type ToolTip}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='HasDropShadow' Value='False'/>
    <Setter Property='Foreground' Value='White'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='FontFamily' Value='{StaticResource UiFont}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ToolTip}'>
          <Border Background='{StaticResource MenuBg}' BorderBrush='{StaticResource MenuBorder}' BorderThickness='1' CornerRadius='7' Padding='9,5,9,6' Margin='4' MaxWidth='360'>
            <ContentPresenter/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='{x:Type ContextMenu}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='FontFamily' Value='{StaticResource UiFont}'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type ContextMenu}'>
          <Border Background='{StaticResource MenuBg}' BorderBrush='{StaticResource MenuBorder}' BorderThickness='1' CornerRadius='9' Padding='5' Margin='8'>
            <Border.Effect>
              <DropShadowEffect BlurRadius='14' ShadowDepth='2' Opacity='0.45'/>
            </Border.Effect>
            <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Cycle'/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType='{x:Type MenuItem}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='Foreground' Value='{StaticResource Text}'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type MenuItem}'>
          <Grid>
            <Border x:Name='Bd' CornerRadius='6' Padding='9,5,12,6' Background='Transparent' MinWidth='150'>
              <Grid>
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width='22'/>
                  <ColumnDefinition Width='*'/>
                  <ColumnDefinition Width='Auto'/>
                  <ColumnDefinition Width='Auto'/>
                </Grid.ColumnDefinitions>
                <ContentPresenter x:Name='Ic' ContentSource='Icon' Width='14' Height='14' VerticalAlignment='Center' HorizontalAlignment='Left'/>
                <Path x:Name='Chk' Grid.Column='0' Data='M20 6 9 17l-5-5' Stroke='{StaticResource Text}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Width='24' Height='24' Stretch='None' Visibility='Collapsed'>
                  <Path.LayoutTransform><ScaleTransform ScaleX='0.6' ScaleY='0.6'/></Path.LayoutTransform>
                </Path>
                <ContentPresenter Grid.Column='1' ContentSource='Header' VerticalAlignment='Center' RecognizesAccessKey='True' Margin='2,0,0,0'/>
                <TextBlock Grid.Column='2' Text='{TemplateBinding InputGestureText}' Foreground='{StaticResource TextFaint}' Margin='18,0,0,0' FontSize='11' VerticalAlignment='Center'/>
                <Path x:Name='Arrow' Grid.Column='3' Data='m9 18 6-6-6-6' Stroke='{StaticResource TextFaint}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Width='24' Height='24' Stretch='None' Margin='8,0,-8,0' Visibility='Collapsed'>
                  <Path.LayoutTransform><ScaleTransform ScaleX='0.6' ScaleY='0.6'/></Path.LayoutTransform>
                </Path>
              </Grid>
            </Border>
            <Popup x:Name='PART_Popup' Placement='Right' HorizontalOffset='-4' VerticalOffset='-8' IsOpen='{Binding IsSubmenuOpen, RelativeSource={RelativeSource TemplatedParent}}' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>
              <Border Background='{StaticResource MenuBg}' BorderBrush='{StaticResource MenuBorder}' BorderThickness='1' CornerRadius='9' Padding='5' Margin='8'>
                <Border.Effect>
                  <DropShadowEffect BlurRadius='14' ShadowDepth='2' Opacity='0.45'/>
                </Border.Effect>
                <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Cycle'/>
              </Border>
            </Popup>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsHighlighted' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Hover}'/>
            </Trigger>
            <Trigger Property='IsSubmenuOpen' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Hover}'/>
            </Trigger>
            <Trigger Property='HasItems' Value='True'>
              <Setter TargetName='Arrow' Property='Visibility' Value='Visible'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Chk' Property='Visibility' Value='Visible'/>
              <Setter TargetName='Ic' Property='Visibility' Value='Collapsed'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter Property='Opacity' Value='0.4'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style TargetType='{x:Type Separator}'>
    <Setter Property='OverridesDefaultStyle' Value='True'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Separator}'>
          <Rectangle Height='1' Fill='{StaticResource Line}' Margin='6,4,6,4'/>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='SChip' TargetType='{x:Type RadioButton}'>
    <Setter Property='Foreground' Value='{StaticResource SText}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Margin' Value='0,0,6,4'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type RadioButton}'>
          <Border x:Name='Bd' CornerRadius='6' Background='{StaticResource SField}' BorderBrush='{StaticResource SLine}' BorderThickness='1' Padding='10,3,10,4'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource SHover}'/>
            </Trigger>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource Accent}'/>
              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Accent}'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='SCheck' TargetType='{x:Type CheckBox}'>
    <Setter Property='Foreground' Value='{StaticResource SText}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type CheckBox}'>
          <StackPanel Orientation='Horizontal' Background='Transparent'>
            <Border x:Name='Box' Width='16' Height='16' CornerRadius='4' Background='{StaticResource SField}' BorderBrush='#FF6A6A70' BorderThickness='1' VerticalAlignment='Center'>
              <Path x:Name='Mark' Data='M3 7.5 5.8 10.3 11 4.5' Stroke='White' StrokeThickness='2' StrokeLineJoin='Round' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Stretch='None' Visibility='Collapsed'/>
            </Border>
            <ContentPresenter Margin='8,0,0,0' VerticalAlignment='Center' RecognizesAccessKey='True'/>
          </StackPanel>
          <ControlTemplate.Triggers>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='Box' Property='Background' Value='{StaticResource Accent}'/>
              <Setter TargetName='Box' Property='BorderBrush' Value='{StaticResource Accent}'/>
              <Setter TargetName='Mark' Property='Visibility' Value='Visible'/>
            </Trigger>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Box' Property='BorderBrush' Value='#FFA0A0A8'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='SKey' TargetType='{x:Type TextBox}'>
    <Setter Property='Foreground' Value='White'/>
    <Setter Property='Background' Value='{StaticResource SField}'/>
    <Setter Property='BorderBrush' Value='{StaticResource SLine}'/>
    <Setter Property='CaretBrush' Value='Transparent'/>
    <Setter Property='SelectionBrush' Value='Transparent'/>
    <Setter Property='IsReadOnly' Value='True'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='FontFamily' Value='Consolas'/>
    <Setter Property='Padding' Value='8,4,8,4'/>
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type TextBox}'>
          <Border x:Name='Bd' CornerRadius='5' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='1' Padding='{TemplateBinding Padding}'>
            <ScrollViewer x:Name='PART_ContentHost' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource SHover}'/>
            </Trigger>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Accent}'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='SFieldBox' TargetType='{x:Type TextBox}' BasedOn='{StaticResource SKey}'>
    <Setter Property='IsReadOnly' Value='False'/>
    <Setter Property='Cursor' Value='IBeam'/>
    <Setter Property='CaretBrush' Value='White'/>
    <Setter Property='SelectionBrush' Value='{StaticResource Accent}'/>
    <Setter Property='FontFamily' Value='{StaticResource UiFont}'/>
  </Style>
  <Style x:Key='SBtn' TargetType='{x:Type Button}'>
    <Setter Property='Foreground' Value='{StaticResource SText}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Focusable' Value='False'/>
    <Setter Property='Padding' Value='14,5,14,6'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' CornerRadius='5' Background='{StaticResource SField}' BorderBrush='{StaticResource SLine}' BorderThickness='1' Padding='{TemplateBinding Padding}'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource SHover}'/>
            </Trigger>
            <Trigger Property='IsPressed' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource SPressed}'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter Property='Opacity' Value='0.4'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='SBtnAccent' TargetType='{x:Type Button}' BasedOn='{StaticResource SBtn}'>
    <Setter Property='Foreground' Value='White'/>
    <Setter Property='Padding' Value='18,5,18,6'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='{x:Type Button}'>
          <Border x:Name='Bd' CornerRadius='5' Background='{StaticResource Accent}' Padding='{TemplateBinding Padding}'>
            <ContentPresenter VerticalAlignment='Center' HorizontalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='Bd' Property='Background' Value='{StaticResource AccentHover}'/>
            </Trigger>
            <Trigger Property='IsEnabled' Value='False'>
              <Setter Property='Opacity' Value='0.4'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <Style x:Key='SBtnDanger' TargetType='{x:Type Button}' BasedOn='{StaticResource SBtn}'>
    <Setter Property='Foreground' Value='{StaticResource SError}'/>
  </Style>
</ResourceDictionary>";

        public static ResourceDictionary Create()
        {
            return (ResourceDictionary)XamlReader.Parse(Xaml);
        }

        public static T Res<T>(string key) where T : class
        {
            return Application.Current.Resources[key] as T;
        }

        /// <summary>Icône vectorielle (clé IcoXxx) à la taille voulue ; la couleur suit la propriété Foreground héritée.</summary>
        public static Viewbox Icon(string key, double size)
        {
            var p = new Path { Style = Res<Style>("Ico"), Data = Res<Geometry>(key) };
            return new Viewbox { Width = size, Height = size, Child = p };
        }

        public static Viewbox Icon(string key, double size, Brush color)
        {
            var vb = Icon(key, size);
            TextElement.SetForeground(vb, color);
            return vb;
        }

        public static Button IconButton(string key, string tip, double iconSize)
        {
            var b = new Button { Style = Res<Style>("IconBtn"), ToolTip = tip };
            b.Content = Icon(key, iconSize);
            return b;
        }

        public static SolidColorBrush Brush(byte a, Color c)
        {
            var b = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
            b.Freeze();
            return b;
        }

        public static SolidColorBrush Argb(uint argb)
        {
            var b = new SolidColorBrush(Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
            b.Freeze();
            return b;
        }
    }
}
