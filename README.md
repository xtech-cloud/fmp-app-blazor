FMP Unity Application（简称UnityApp）作为FMP方案中的视觉呈现终端部分，能灵活的搭配使用FMP方案构建出的标准模块，快速制作数字内容的交互终端。

# DD (Design Document)

## Blazor应用程序 （FMP-BlazorApp）

### 运行时伸缩（Runtime Scaling）


# DG (Development Guide)

## Blazor应用程序 （FMP-BlazorApp）

### 配置虚拟环境

# UM (User Manual)

## Blazor应用程序 （FMP-BlazorApp）

TODO











# 模块使用


- 修改wwwroot/data/menu.json
- 修改wwwroot/data/modules.json
- 将模块库文件拷贝到wwwroot/modules/目录下

# 要点记录

- 发布为WASM后，提示程序集找不到
  ```
  System.TypeLoadException: Could not resolve type with token from typeref (expected class 'System.Xml.Serialization.XmlArrayAttribute' in assembly 'netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51')
  ```
  Blazor在发布WASM时会默认开启链接器的裁剪，导致在动态加载程序集时部分类找不到。
  按照文档使用BlazorWebAssemblyEnableLinking和BlazorLinkerDescriptor均不起作用，后改为PublishTrimmed解决，但最后发布的文件体积增加了约20M
  文档参考：
  https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/configure-linker?view=aspnetcore-3.1
  https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options?pivots=dotnet-6-0
