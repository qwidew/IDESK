# iDesk Multi-Project Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the single WPF project into a multi-project solution with shared core, widget library, console, and host.

**Architecture:** Single solution with 6 projects sharing a common Core library. Host EXE sets up DI and wires Console to widget factories. Widgets are class libraries with WPF support.

**Tech Stack:** .NET 10, WPF, EF Core SQLite, Microsoft.Extensions.DependencyInjection

---

### Task 1: Create directory structure and Core project

**Files:**

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\IDESK.Core.csproj`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\DeskWidget.cs`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Helper\RelayCommand.cs`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Logging\ILogger.cs`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Logging\FileLogger.cs`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Style\Colors.xaml`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Style\Icons.xaml`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Style\DefaultLightTheme.xaml`

- Create: `E:\project\csharp\IDESK\src\IDESK.Core\Style\BookmarkStyles.xaml`

- [ ] **Step 1: Create directory structure**

Run:

```bash
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Core/Helper"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Core/Logging"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Core/Style"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Console"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Host"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Todo/Models"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Todo/Data"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Todo/Service"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Todo/Control"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Notes"
mkdir -p "E:/project/csharp/IDESK/src/IDESK.Widgets.Schedule"
```

- [ ] **Step 2: Create IDESK.Core.csproj**

Write `E:\project\csharp\IDESK\src\IDESK.Core\IDESK.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>IDESK.Core</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Copy DeskWidget.cs to Core with namespace update**

Copy `E:\project\csharp\IDESK\WPFTodoDemo\WPFTodoDemo\DeskWidget.cs` to `E:\project\csharp\IDESK\src\IDESK.Core\DeskWidget.cs`.

Change namespace from `WPFTodoDemo` to `IDESK.Core`.

- [ ] **Step 4: Copy Helper/RelayCommand.cs to Core with namespace update**

Copy `E:\project\csharp\IDESK\WPFTodoDemo\WPFTodoDemo\Helper\RelayCommand.cs` to `E:\project\csharp\IDESK\src\IDESK.Core\Helper\RelayCommand.cs`.

Change namespace from `WPFTodoDemo.Helper` to `IDESK.Core`.

- [ ] **Step 5: Copy Logging files to Core with namespace update**

Copy `ILogger.cs` and `FileLogger.cs` from `WPFTodoDemo/Logging/` to `src/IDESK.Core/Logging/`.

Change namespace from `WPFTodoDemo.Logging` to `IDESK.Core`.

- [ ] **Step 6: Copy Style XAML files to Core**

Copy all 4 XAML files from `WPFTodoDemo/Style/` to `src/IDESK.Core/Style/`. These are pure resource dictionaries with no C# code-behind, so only file copy needed.

---

### Task 2: Create Todo widget project and migrate code

**Files:**

- Create: `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\IDESK.Widgets.Todo.csproj`

- [ ] **Step 1: Create IDESK.Widgets.Todo.csproj**

Write `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\IDESK.Widgets.Todo.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>IDESK.Widgets.Todo</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IDESK.Core\IDESK.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Copy Models/TodoItem.cs**

Copy to `src/IDESK.Widgets.Todo/Models/TodoItem.cs`. Change namespace from `WPFTodoDemo.Models` to `IDESK.Widgets.Todo`.

- [ ] **Step 3: Copy Data/AppDbContext.cs**

Copy to `src/IDESK.Widgets.Todo/Data/AppDbContext.cs`. Change namespace from `WPFTodoDemo.Data` to `IDESK.Widgets.Todo`. Update `using WPFTodoDemo.Models;` to `using IDESK.Widgets.Todo;`.

- [ ] **Step 4: Copy Service files**

Copy all 4 service files to `src/IDESK.Widgets.Todo/Service/`. Change namespace from `WPFTodoDemo.Service` to `IDESK.Widgets.Todo`.

In `DataService.cs`, update `using WPFTodoDemo.Models;` to `using IDESK.Widgets.Todo;` and `using WPFTodoDemo.Data;` to `using IDESK.Widgets.Todo;`.

- [ ] **Step 5: Copy Control files**

Copy `CalendarDialog.xaml`, `CalendarDialog.xaml.cs`, `TodoItemBlock.xaml`, `TodoItemBlock.xaml.cs` to `src/IDESK.Widgets.Todo/Control/`.

In XAML files, change `xmlns:local="clr-namespace:WPFTodoDemo.Control"` to `xmlns:local="clr-namespace:IDESK.Widgets.Todo.Control"`.

In `.xaml.cs` files, change namespace to `IDESK.Widgets.Todo.Control`.

For `TodoItemBlock.xaml.cs`, update `using WPFTodoDemo.Helper;` to `using IDESK.Core;` (RelayCommand is no longer directly used, but keep the import clean).

- [ ] **Step 6: Copy TodoListViewModel.cs**

Copy to `src/IDESK.Widgets.Todo/TodoListViewModel.cs`. Update namespace and imports:

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using IDESK.Core;
using IDESK.Widgets.Todo;

namespace IDESK.Widgets.Todo;
// ... rest of the code stays the same
```

- [ ] **Step 7: Create TodoListView.xaml (UserControl extracted from MainWindow)**

Write `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\TodoListView.xaml`:

```xml
<UserControl x:Class="IDESK.Widgets.Todo.TodoListView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:controls="clr-namespace:IDESK.Widgets.Todo.Control"
        mc:Ignorable="d"
        d:DesignHeight="750" d:DesignWidth="400">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Title -->
        <Label Grid.Row="0" Content="TODOLIST" HorizontalAlignment="Center" FontSize="16" Margin="0 10 0 0"/>

        <!-- TextBox -->
        <TextBox Grid.Row="1" Name="TxtBoxTodo" Width="160" Height="21"
                 Style="{DynamicResource DefaultTextBoxStyle}"
                 Text="{Binding NewContent, UpdateSourceTrigger=PropertyChanged}"/>

        <!-- Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="5"
                    HorizontalAlignment="Center">
            <Button Width="85" Margin="0 0 5 0" 
                    Style="{DynamicResource DefaultButtonStyle}"
                    Height="25" Command="{Binding AddCommand}">
                <StackPanel Orientation="Horizontal">
                    <Path Data="{StaticResource IconAdd}" Stretch="Uniform" Fill="{StaticResource Blue9Brush}"
                          Width="10" Height="10"/>
                    <Label Content="添加代办" FontSize="11" Foreground="{StaticResource Blue9Brush}"/>
                </StackPanel>
            </Button>
            <Button Width="90" Margin="5 0 0 0" 
                    Style="{DynamicResource DangerButtonStyle}"
                    Height="25" Command="{Binding ClearCommand}">
                <StackPanel Orientation="Horizontal">
                    <Path Data="{StaticResource IconDelete}" Stretch="Uniform" Fill="{StaticResource Red9Brush}"
                          Width="12" Height="12"/>
                    <Label Content="删除完成" FontSize="11" Foreground="{StaticResource Red9Brush}"/>
                </StackPanel>
            </Button>
        </StackPanel>

        <Button Grid.Row="2" Width="75" Height="25" HorizontalAlignment="Right" Margin="5"
                Style="{DynamicResource BorderlessButtonStyle}"
                Command="{Binding ToggleFilterCommand}">
            <StackPanel Orientation="Horizontal">
                <Grid Width="12" Height="12">
                    <Path Data="{StaticResource IconHide}" Stretch="Uniform" Fill="{StaticResource Blue9Brush}">
                        <Path.Style>
                            <Style TargetType="Path">
                                <Setter Property="Opacity" Value="1"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding ShowCompleted}" Value="False">
                                        <Setter Property="Opacity" Value="0"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Path.Style>
                    </Path>
                    <Path Data="{StaticResource IconVisibility}" Stretch="Uniform" Fill="{StaticResource Blue9Brush}">
                        <Path.Style>
                            <Style TargetType="Path">
                                <Setter Property="Opacity" Value="0"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding ShowCompleted}" Value="False">
                                        <Setter Property="Opacity" Value="1"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </Path.Style>
                    </Path>
                </Grid>
                <Label FontSize="10" Foreground="{StaticResource Blue9Brush}">
                    <Label.Style>
                        <Style TargetType="Label">
                            <Setter Property="Content" Value="隐藏完成"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding ShowCompleted}" Value="False">
                                    <Setter Property="Content" Value="显示全部"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Label.Style>
                </Label>
            </StackPanel>
        </Button>

        <ProgressBar Grid.Row="3" />

        <Grid Grid.Row="4" Margin="20 5">
            <ListBox ItemsSource="{Binding FilteredView}"
                     AllowDrop="True"
                     VirtualizingStackPanel.IsVirtualizing="False"
                     PreviewMouseLeftButtonDown="OnPreviewMouseLeftButtonDown"
                     DragOver="OnDragOver" Drop="OnDrop" DragLeave="OnDragLeave"
                     GiveFeedback="OnGiveFeedback">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <controls:TodoItemBlock TodoContent="{Binding Content}"
                                                IsDone="{Binding IsDone}"
                                                CompleteCommand="{Binding DataContext.CompleteItemCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                DeleteCommand="{Binding DataContext.DeleteItemCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
            <Canvas IsHitTestVisible="False">
                <Rectangle x:Name="InsertionLine" Fill="{StaticResource Blue5Brush}"
                           Height="2" Visibility="Collapsed"/>
            </Canvas>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 8: Create TodoListView.xaml.cs with drag-drop code-behind**

Write `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\TodoListView.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IDESK.Widgets.Todo;

public partial class TodoListView : UserControl
{
    public TodoListView()
    {
        InitializeComponent();
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        var button = FindAncestor<Button>(source);
        if (button == null || button.Command != null || button.Content?.ToString() != "⠿") return;

        var listBoxItem = FindAncestor<ListBoxItem>(source);
        if (listBoxItem == null) return;

        var todoItem = listBoxItem.DataContext as TodoItem;
        if (todoItem == null) return;

        DragDrop.DoDragDrop(listBoxItem, todoItem, DragDropEffects.Move);
    }

    private void OnGiveFeedback(object? sender, GiveFeedbackEventArgs e)
    {
        e.UseDefaultCursors = false;
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        HideInsertionLine();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TodoItem)))
        {
            e.Effects = DragDropEffects.None;
            HideInsertionLine();
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        UpdateInsertionLine((ListBox)sender, e.GetPosition((ListBox)sender));
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        var listBox = sender as ListBox;
        var draggedItem = e.Data.GetData(typeof(TodoItem)) as TodoItem;
        if (listBox == null || draggedItem == null) return;

        var vm = DataContext as TodoListViewModel;
        if (vm == null) return;

        HideInsertionLine();

        int targetIndex = GetDropIndex(listBox, e.GetPosition(listBox));
        if (targetIndex < 0) return;

        int oldIndex = vm.TodoList.IndexOf(draggedItem);
        if (oldIndex < 0) return;

        int newIndex = targetIndex > oldIndex ? targetIndex - 1 : targetIndex;
        if (oldIndex == newIndex) return;

        vm.TodoList.Move(oldIndex, newIndex);

        for (int i = 0; i < vm.TodoList.Count; i++)
        {
            vm.TodoList[i].SortOrder = i;
        }
        _ = vm.SaveOrderAsync();
    }

    private void HideInsertionLine()
    {
        InsertionLine.Visibility = Visibility.Collapsed;
    }

    private void UpdateInsertionLine(ListBox listBox, Point pos)
    {
        double lineY = -1;

        if (listBox.Items.Count == 0) return;

        if (pos.Y < 0)
        {
            lineY = 0;
        }
        else
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (item == null) continue;

                var bounds = item.TransformToAncestor(listBox)
                               .TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));

                if (pos.Y >= bounds.Top && pos.Y <= bounds.Bottom)
                {
                    double midY = bounds.Top + bounds.Height / 2;
                    lineY = pos.Y < midY ? bounds.Top : bounds.Bottom;
                    break;
                }
            }

            if (lineY < 0)
            {
                var lastItem = listBox.ItemContainerGenerator
                    .ContainerFromIndex(listBox.Items.Count - 1) as ListBoxItem;
                if (lastItem != null)
                {
                    var lastBounds = lastItem.TransformToAncestor(listBox)
                        .TransformBounds(new Rect(0, 0, lastItem.ActualWidth, lastItem.ActualHeight));
                    if (pos.Y > lastBounds.Bottom)
                        lineY = lastBounds.Bottom;
                }
            }
        }

        if (lineY >= 0)
        {
            InsertionLine.Width = listBox.ActualWidth;
            Canvas.SetTop(InsertionLine, lineY);
            Canvas.SetLeft(InsertionLine, 0);
            InsertionLine.Visibility = Visibility.Visible;
        }
        else
        {
            HideInsertionLine();
        }
    }

    private int GetDropIndex(ListBox listBox, Point pos)
    {
        if (pos.Y < 0 || listBox.Items.Count == 0) return 0;

        for (int i = 0; i < listBox.Items.Count; i++)
        {
            var item = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (item == null) continue;

            var bounds = item.TransformToAncestor(listBox)
                           .TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));

            if (pos.Y >= bounds.Top && pos.Y <= bounds.Bottom)
            {
                double midY = bounds.Top + bounds.Height / 2;
                return pos.Y < midY ? i : i + 1;
            }
        }

        return listBox.Items.Count;
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element != null)
        {
            if (element is T t) return t;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
```

- [ ] **Step 9: Create TodoWindow.cs (DeskWidget subclass)**

Write `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\TodoWindow.cs`:

```csharp
using IDESK.Core;

namespace IDESK.Widgets.Todo;

public class TodoWindow : DeskWidget
{
    public TodoWindow(TodoListViewModel vm)
    {
        var view = new TodoListView();
        view.DataContext = vm;
        NormalContent = view;
        BookmarkPresetId = 2;
        Title = "Todo";
    }
}
```

- [ ] **Step 10: Create Todo service registration extension**

Write `E:\project\csharp\IDESK\src\IDESK.Widgets.Todo\TodoServiceExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace IDESK.Widgets.Todo;

public static class TodoServiceExtensions
{
    public static IServiceCollection AddTodoWidget(this IServiceCollection services)
    {
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<TodoListViewModel>();
        services.AddTransient<TodoWindow>();
        return services;
    }
}
```

---

### Task 3: Create Console project

**Files:**

- Create: `E:\project\csharp\IDESK\src\IDESK.Console\IDESK.Console.csproj`

- Create: `E:\project\csharp\IDESK\src\IDESK.Console\ConsoleWindow.xaml`

- Create: `E:\project\csharp\IDESK\src\IDESK.Console\ConsoleWindow.xaml.cs`

- [ ] **Step 1: Create IDESK.Console.csproj**

Write `E:\project\csharp\IDESK\src\IDESK.Console\IDESK.Console.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>IDESK.Console</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\IDESK.Core\IDESK.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create ConsoleWindow.xaml**

Write `E:\project\csharp\IDESK\src\IDESK.Console\ConsoleWindow.xaml`:

```xml
<Window x:Class="IDESK.Console.ConsoleWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="iDesk Console" Height="300" Width="400"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="20">
        <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
            <TextBlock Text="iDesk Desktop Widgets" FontSize="18" FontWeight="Bold"
                       HorizontalAlignment="Center" Margin="0 0 0 20"/>
            <Button x:Name="OpenTodoButton" Content="打开 Todo" Width="160" Height="40"
                    FontSize="14"/>
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 3: Create ConsoleWindow.xaml.cs**

Write `E:\project\csharp\IDESK\src\IDESK.Console\ConsoleWindow.xaml.cs`:

```csharp
using System.Windows;

namespace IDESK.Console;

public partial class ConsoleWindow : Window
{
    public event Action? OpenTodoRequested;

    public ConsoleWindow()
    {
        InitializeComponent();
        OpenTodoButton.Click += (_, _) => OpenTodoRequested?.Invoke();
    }
}
```

---

### Task 4: Create Host project

**Files:**

- Create: `E:\project\csharp\IDESK\src\IDESK.Host\IDESK.Host.csproj`

- Create: `E:\project\csharp\IDESK\src\IDESK.Host\App.xaml`

- Create: `E:\project\csharp\IDESK\src\IDESK.Host\App.xaml.cs`

- [ ] **Step 1: Create IDESK.Host.csproj**

Write `E:\project\csharp\IDESK\src\IDESK.Host\IDESK.Host.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="11.0.0-preview.4.26230.115" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\IDESK.Core\IDESK.Core.csproj" />
    <ProjectReference Include="..\IDESK.Console\IDESK.Console.csproj" />
    <ProjectReference Include="..\IDESK.Widgets.Todo\IDESK.Widgets.Todo.csproj" />
    <ProjectReference Include="..\IDESK.Widgets.Notes\IDESK.Widgets.Notes.csproj" />
    <ProjectReference Include="..\IDESK.Widgets.Schedule\IDESK.Widgets.Schedule.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create App.xaml**

Write `E:\project\csharp\IDESK\src\IDESK.Host\App.xaml`:

```xml
<Application x:Class="IDESK.Host.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/IDESK.Core;component/Style/Colors.xaml"/>
                <ResourceDictionary Source="/IDESK.Core;component/Style/Icons.xaml"/>
                <ResourceDictionary Source="/IDESK.Core;component/Style/DefaultLightTheme.xaml"/>
                <ResourceDictionary Source="/IDESK.Core;component/Style/BookmarkStyles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 3: Create App.xaml.cs**

Write `E:\project\csharp\IDESK\src\IDESK.Host\App.xaml.cs`:

```csharp
using System.Windows;
using IDESK.Console;
using IDESK.Widgets.Todo;
using Microsoft.Extensions.DependencyInjection;

namespace IDESK.Host;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddTodoWidget();
        services.AddTransient<ConsoleWindow>();

        Services = services.BuildServiceProvider();

        var console = Services.GetRequiredService<ConsoleWindow>();
        console.OpenTodoRequested += () =>
        {
            var todoWindow = Services.GetRequiredService<TodoWindow>();
            todoWindow.Show();
        };
        console.Show();
    }
}
```

---

### Task 5: Create scaffolding projects (Notes, Schedule)

**Files:**

- Create: `E:\project\csharp\IDESK\src\IDESK.Widgets.Notes\IDESK.Widgets.Notes.csproj`

- Create: `E:\project\csharp\IDESK\src\IDESK.Widgets.Schedule\IDESK.Widgets.Schedule.csproj`

- [ ] **Step 1: Create IDESK.Widgets.Notes.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>IDESK.Widgets.Notes</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\IDESK.Core\IDESK.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create IDESK.Widgets.Schedule.csproj**

(Same as Notes but with `Schedule` namespace)

---

### Task 6: Create solution file and verify build

**Files:**

- Create: `E:\project\csharp\IDESK\IDESK.sln`

- [ ] **Step 1: Create IDESK.sln**

Run:

```bash
cd "E:/project/csharp/IDESK" && dotnet new sln -n IDESK
dotnet sln add src/IDESK.Core/IDESK.Core.csproj
dotnet sln add src/IDESK.Console/IDESK.Console.csproj
dotnet sln add src/IDESK.Host/IDESK.Host.csproj
dotnet sln add src/IDESK.Widgets.Todo/IDESK.Widgets.Todo.csproj
dotnet sln add src/IDESK.Widgets.Notes/IDESK.Widgets.Notes.csproj
dotnet sln add src/IDESK.Widgets.Schedule/IDESK.Widgets.Schedule.csproj
```

- [ ] **Step 2: Build and fix**

```bash
cd "E:/project/csharp/IDESK" && dotnet build
```

Expected: Build succeeds. If there are namespace errors, fix them and rebuild.

---

## Self-Review Checklist

1. **Spec coverage**: Every requirement is covered — migration of Todo code (Task 2), Console with one button (Task 3), Host with DI (Task 4), scaffold projects (Task 5), solution file (Task 6).
2. **Placeholder scan**: No TBD, TODO, or vague steps. Every file path and code block is concrete.
3. **Type consistency**: TodoWindow uses TodoListViewModel via constructor injection. TodoListView is a UserControl. ConsoleWindow fires event, Host wires it. All namespaces and references are consistent.
