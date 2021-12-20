# Web应用开发教程 - Part 5: 授权
````json
//[doc-params]
{
    "UI": ["MVC","Blazor","BlazorServer","NG"],
    "DB": ["EF","Mongo"]
}
````
## 关于本教程

在这个教程系列中，你将创建一个基于ABP的Web应用程序，叫 `Acme.BookStore`。这个应用程序是用来管理书籍及其作者的列表的。它是使用以下技术开发：

* **{{DB_Value}}** 作为ORM的提供者。 
* **{{UI_Value}}** 作为UI框架。

本教程由以下几个部分组成：

- [第一部分：创建服务器端](Part-1.md)
- [第2部分：书籍列表页](Part-2.md)
- [第3部分：创建、更新和删除书籍](Part-3.md)
- [第四部分：集成测试](Part-4.md)
- **第5部分：授权（本部分）**
- [第六部分：作者：领域层](Part-6.md)
- [第七部分：作者：数据库集成](Part-7.md)
- [第8部分。作者：应用层](Part-8.md)
- [第九部分：作者：用户界面](Part-9.md)
- [第10部分：书与作者关联](Part-10.md)

### 下载源代码

This tutorial has multiple versions based on your **UI** and **Database** preferences. We've prepared a few combinations of the source code to be downloaded:

本教程根据你对**UI**和**Database**的偏好有多个版本。我们准备了几个组合的源代码供大家下载：

* [MVC (Razor Pages) UI with EF Core](https://github.com/abpframework/abp-samples/tree/master/BookStore-Mvc-EfCore)
* [Blazor UI with EF Core](https://github.com/abpframework/abp-samples/tree/master/BookStore-Blazor-EfCore)
* [Angular UI with MongoDB](https://github.com/abpframework/abp-samples/tree/master/BookStore-Angular-MongoDb)

> 如果你在Windows上遇到 "文件名太长 "或 "解压错误"，它可能与Windows的最大文件路径限制有关。Windows有一个最大的文件路径限制，即250个字符。要解决这个问题，[在Windows 10中启用长路径选项](https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=cmd#enable-long-paths-in-windows-10-version-1607-and-later)。

> 如果你遇到与Git有关的长路径错误，可以尝试用以下命令在Windows中启用长路径。见 https://github.com/msysgit/msysgit/wiki/Git-cannot-create-a-file-or-directory-with-a-long-path
> `git config --system core.longpaths true`

{{if UI == "MVC" && DB == "EF"}}

### 视频教程

这一部分也被录制成视频教程，并**<a href="https://www.youtube.com/watch?v=1WsfMITN_Jk&list=PLsNclT2aHJcPNaCf7Io3DbMN6yAk_DgWJ&index=5" target="_blank">发布在YouTube上</a>**。

{{end}}

## 授权

ABP框架提供了一个基于ASP.NET Core的[授权基础设施](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction)的[授权系统](./Authorization.md)。在标准授权基础架构之上增加的一个主要功能是**权限系统**，它允许定义权限并启用/禁用每个角色、用户或客户端。

### 权限命名

一个权限必须有一个唯一的名字( `string`类型). 最好的方法是把它定义为`const`变量, 这样我们就可以重复使用权限的名称。

打开`Acme.BookStore.Application.Contracts`项目中的`BookStorePermissions`类（在`Permissions`文件夹中），修改内容如下所示：

````csharp
namespace Acme.BookStore.Permissions
{
    public static class BookStorePermissions
    {
        public const string GroupName = "BookStore";

        public static class Books
        {
            public const string Default = GroupName + ".Books";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
        }
    }
}
````

This is a hierarchical way of defining permission names. For example, "create book" permission name was defined as `BookStore.Books.Create`. ABP doesn't force you to a structure, but we find this way useful.

这是一种定义权限名称的划分层级方式。例如，**新建书籍**的权限名称被定义为`BookStore.Books.Create`。ABP并不强迫你使用结构的方式，但我们发现这种方式很有用。

### 权限定义

你应该在使用权限之前定义它们。

打开`Acme.BookStore.Application.Contracts`项目中的`BookStorePermissionDefinitionProvider`类（在`Permissions`文件夹中），修改内容如下所示：

````csharp
using Acme.BookStore.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Acme.BookStore.Permissions
{
    public class BookStorePermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var bookStoreGroup = context.AddGroup(BookStorePermissions.GroupName, L("Permission:BookStore"));

            var booksPermission = bookStoreGroup.AddPermission(BookStorePermissions.Books.Default, L("Permission:Books"));
            booksPermission.AddChild(BookStorePermissions.Books.Create, L("Permission:Books.Create"));
            booksPermission.AddChild(BookStorePermissions.Books.Edit, L("Permission:Books.Edit"));
            booksPermission.AddChild(BookStorePermissions.Books.Delete, L("Permission:Books.Delete"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<BookStoreResource>(name);
        }
    }
}
````

这个类定义了一个**权限组**（在用户界面上进行权限分组，下面会看到）和这个组内的**4个权限**。另外，**创建**，**编辑**和**删除**是 `BookStorePermissions.Books.Default` 权限的子权限。**只有当父权限被选中时**，子权限才能被选中。

最后，编辑本地化文件（`en.json`在`Acme.BookStore.Domain.Shared`项目的`Localization/BookStore`文件夹下）定义上面使用的本地化键值对。

````json
"Permission:BookStore": "Book Store",
"Permission:Books": "Book Management",
"Permission:Books.Create": "Creating new books",
"Permission:Books.Edit": "Editing the books",
"Permission:Books.Delete": "Deleting the books"
````

> 本地化键的名称是随意的，没有强制的规则。但我们更喜欢上面的使用习惯。

### 权限管理界面

一旦你定义了权限，你可以在**权限管理**中看到它们。

进入*管理->身份->角色*页面，点击管理员角色的*权限*按钮，打开权限管理：

![bookstore-permissions-ui](images/bookstore-permissions-ui.png)

授予你想要的权限并保存。

> **提示**: 如果你运行`Acme.BookStore.DbMigrator` 应用程序，新的权限会自动授予管理员角色。

## 授权

现在，你可以使用权限来授权书籍管理。

### 应用层 & HTTP API

打开`BookAppService`类，按照上面定义的权限名称添加并设置策略名称：

````csharp
using System;
using Acme.BookStore.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Acme.BookStore.Books
{
    public class BookAppService :
        CrudAppService<
            Book, //The Book entity
            BookDto, //Used to show books
            Guid, //Primary key of the book entity
            PagedAndSortedResultRequestDto, //Used for paging/sorting
            CreateUpdateBookDto>, //Used to create/update a book
        IBookAppService //implement the IBookAppService
    {
        public BookAppService(IRepository<Book, Guid> repository)
            : base(repository)
        {
            GetPolicyName = BookStorePermissions.Books.Default;
            GetListPolicyName = BookStorePermissions.Books.Default;
            CreatePolicyName = BookStorePermissions.Books.Create;
            UpdatePolicyName = BookStorePermissions.Books.Edit;
            DeletePolicyName = BookStorePermissions.Books.Delete;
        }
    }
}
````

在构造函数中添加了代码。基类`CrudAppService`在CRUD操作中自动使用这些权限。这使得**应用服务**安全，同时也使得**HTTP API**安全，因为这个服务被自动用作HTTP API，正如之前解释的那样（参考[auto API controllers](./API/Auto-API-Controllers.md)）。

> 后续在开发作者管理功能时，你会看到使用`[Authorize(...)]`属性的声明式授权。

{{if UI == "MVC"}}

### Razor Page

虽然HTTP API和应用服务的安全防护可以防止未经授权的用户使用这些服务，但他们仍然可以导航到图书管理页面。当页面对服务端进行第一次AJAX调用时，他们会得到授权异常，我们也应该对页面进行授权，以获得更好的用户体验和安全。

打开`BookStoreWebModule` ，在`ConfigureServices`方法中添加以下代码块：

````csharp
Configure<RazorPagesOptions>(options =>
{
    options.Conventions.AuthorizePage("/Books/Index", BookStorePermissions.Books.Default);
    options.Conventions.AuthorizePage("/Books/CreateModal", BookStorePermissions.Books.Create);
    options.Conventions.AuthorizePage("/Books/EditModal", BookStorePermissions.Books.Edit);
});
````

现在，未经授权的用户被重定向到**登录页面**。

#### 隐藏新建书籍按钮

书籍管理页面有一个*新建书籍*按钮，如果当前用户没有*图书创建*权限，该按钮应该是不可见的。

![bookstore-new-book-button-small](images/bookstore-new-book-button-small.png)

打开`Pages/Books/Index.cshtml`文件，修改内容如下：

````html
@page
@using Acme.BookStore.Localization
@using Acme.BookStore.Permissions
@using Acme.BookStore.Web.Pages.Books
@using Microsoft.AspNetCore.Authorization
@using Microsoft.Extensions.Localization
@model IndexModel
@inject IStringLocalizer<BookStoreResource> L
@inject IAuthorizationService AuthorizationService
@section scripts
{
    <abp-script src="/Pages/Books/Index.js"/>
}

<abp-card>
    <abp-card-header>
        <abp-row>
            <abp-column size-md="_6">
                <abp-card-title>@L["Books"]</abp-card-title>
            </abp-column>
            <abp-column size-md="_6" class="text-right">
                @if (await AuthorizationService.IsGrantedAsync(BookStorePermissions.Books.Create))
                {
                    <abp-button id="NewBookButton"
                                text="@L["NewBook"].Value"
                                icon="plus"
                                button-type="Primary"/>
                }
            </abp-column>
        </abp-row>
    </abp-card-header>
    <abp-card-body>
        <abp-table striped-rows="true" id="BooksTable"></abp-table>
    </abp-card-body>
</abp-card>
````

* 添加 `@inject IAuthorizationService AuthorizationService` 来访问授权服务。
* 使用`@if (await AuthorizationService.IsGrantedAsync(BookStorePermissions.Books.Create))`检查图书创建权限，条件判定的形式呈现*新建书籍*按钮。

### JavaScript

图书管理页面中的图书列表，每一行都有一个操作按钮。该行为按钮包含*编辑*和*删除*动作。

![bookstore-edit-delete-actions](images/bookstore-edit-delete-actions.png)

如果当前用户没有授予相关权限，我们应该隐藏这个动作。数据表行操作有一个`visible`选项，可以设置为`false`来隐藏动作项。

打开`Acme.BookStore.Web`项目中的`Pages/Books/Index.js`，为`Edit`动作添加一个`visible`选项，如下图所示：

````js
{
    text: l('Edit'),
    visible: abp.auth.isGranted('BookStore.Books.Edit'), //CHECK for the PERMISSION
    action: function (data) {
        editModal.open({ id: data.record.id });
    }
}
````

对动作`Delete`做同样的处理：

````js
visible: abp.auth.isGranted('BookStore.Books.Delete')
````

* `abp.auth.isGranted(...)`是被用来检查之前定义的权限。
* `visible`可以得到一个函数返回的`bool`值，该值将在以后根据某些条件进行计算。

### 菜单项

即使我们已经对图书管理页面做了全部方面防护，它仍然在应用程序的主菜单上可见。如果当前用户没有权限，我们应该隐藏该菜单项。

打开`BookStoreMenuContributor`类，找到下面的代码块：

````csharp
context.Menu.AddItem(
    new ApplicationMenuItem(
        "BooksStore",
        l["Menu:BookStore"],
        icon: "fa fa-book"
    ).AddItem(
        new ApplicationMenuItem(
            "BooksStore.Books",
            l["Menu:Books"],
            url: "/Books"
        )
    )
);
````

并用以下内容替换代码块：

````csharp
var bookStoreMenu = new ApplicationMenuItem(
    "BooksStore",
    l["Menu:BookStore"],
    icon: "fa fa-book"
);

context.Menu.AddItem(bookStoreMenu);

//CHECK the PERMISSION
if (await context.IsGrantedAsync(BookStorePermissions.Books.Default))
{
    bookStoreMenu.AddItem(new ApplicationMenuItem(
        "BooksStore.Books",
        l["Menu:Books"],
        url: "/Books"
    ));
}
````

你还需要为`ConfigureMenuAsync`方法添加`async`关键字并重组返回值。最终的`BookStoreMenuContributor`类应该是下面这样的：

````csharp
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Acme.BookStore.Localization;
using Acme.BookStore.MultiTenancy;
using Acme.BookStore.Permissions;
using Volo.Abp.TenantManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;

namespace Acme.BookStore.Web.Menus
{
    public class BookStoreMenuContributor : IMenuContributor
    {
        public async Task ConfigureMenuAsync(MenuConfigurationContext context)
        {
            if (context.Menu.Name == StandardMenus.Main)
            {
                await ConfigureMainMenuAsync(context);
            }
        }

        private async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
        {
            if (!MultiTenancyConsts.IsEnabled)
            {
                var administration = context.Menu.GetAdministration();
                administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
            }

            var l = context.GetLocalizer<BookStoreResource>();

            context.Menu.Items.Insert(0, new ApplicationMenuItem("BookStore.Home", l["Menu:Home"], "~/"));

            var bookStoreMenu = new ApplicationMenuItem(
                "BooksStore",
                l["Menu:BookStore"],
                icon: "fa fa-book"
            );

            context.Menu.AddItem(bookStoreMenu);

            //CHECK the PERMISSION
            if (await context.IsGrantedAsync(BookStorePermissions.Books.Default))
            {
                bookStoreMenu.AddItem(new ApplicationMenuItem(
                    "BooksStore.Books",
                    l["Menu:Books"],
                    url: "/Books"
                ));
            }
        }
    }
}
````

{{else if UI == "NG"}}

### Angular Guard配置

UI的第一步是防止未经授权的用户看到 "书籍 "菜单项并进入书籍管理页面。

打开`/src/app/book/book-routing.module.ts`并替换为以下内容：

````js
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard, PermissionGuard } from '@abp/ng.core';
import { BookComponent } from './book.component';

const routes: Routes = [
  { path: '', component: BookComponent, canActivate: [AuthGuard, PermissionGuard] },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class BookRoutingModule {}
````

* 从`@abp/ng.core`中导入了`AuthGuard`和`PermissionGuard`。
* 添加`canActivate: [AuthGuard, PermissionGuard]`到路由定义中。

打开`/src/app/route.provider.ts`，在`/books`路由中添加`requiredPolicy: 'BookStore.Books'`。`/books`路由块应该如下：

````js
{
  path: '/books',
  name: '::Menu:Books',
  parentName: '::Menu:BookStore',
  layout: eLayoutType.application,
  requiredPolicy: 'BookStore.Books',
}
````

### 隐藏新建书籍按钮

图书管理页面有一个*新建书籍*按钮，如果当前用户没有*书籍创建*权限，该按钮应该是不可见的。

![bookstore-new-book-button-small](images/bookstore-new-book-button-small.png)

打开`/src/app/book/book.component.html`文件，替换新建按钮的HTML内容，如下所示：

````html
<!-- Add the abpPermission directive -->
<button *abpPermission="'BookStore.Books.Create'" id="create" class="btn btn-primary" type="button" (click)="createBook()">
  <i class="fa fa-plus mr-1"></i>
  <span>{%{{{ '::NewBook' | abpLocalization }}}%}</span>
</button>
````

* 仅仅添加`*abpPermission="'BookStore.Books.Create'"`，如果当前用户没有权限，就会隐藏这个按钮。

### 隐藏编辑和删除操作

图书管理页面中的图书列表，每一行都有一个操作按钮。该操作按钮包括*编辑*和*删除*操作：

![bookstore-edit-delete-actions](images/bookstore-edit-delete-actions.png)

如果当前用户没有授予相关权限，我们应该隐藏这个操作。

打开`/src/app/book/book.component.html`文件，替换编辑和删除按钮的内容，如下所示：

````html
<!-- Add the abpPermission directive -->
<button *abpPermission="'BookStore.Books.Edit'" ngbDropdownItem (click)="editBook(row.id)">
  {%{{{ '::Edit' | abpLocalization }}}%}
</button>

<!-- Add the abpPermission directive -->
<button *abpPermission="'BookStore.Books.Delete'" ngbDropdownItem (click)="delete(row.id)">
  {%{{{ '::Delete' | abpLocalization }}}%}
</button>
````

* 添加`*abpPermission="'BookStore.Books.Edit'"`，如果当前用户没有编辑权限，会隐藏编辑操作。
* 添加`*abpPermission="'BookStore.Books.Delete'"`，如果当前用户没有删除权限，会隐藏删除操作。

{{else if UI == "Blazor"}}

### 授权Razor组件

打开`Acme.BookStore.Blazor`项目中的`/Pages/Books.razor`文件，在`@page`指令和以下命名空间导入（`@using`行）之间添加一个`Authorize`属性，如下所示：

````html
@page "/books"
@attribute [Authorize(BookStorePermissions.Books.Default)]
@using Acme.BookStore.Permissions
@using Microsoft.AspNetCore.Authorization
...
````

如果当前用户没有登录或没有授予指定的权限，添加这个属性可以防止进入这个页面。尝试进入时，用户会被重定向到登录页面。

### 显示/隐藏操作

书籍管理页面有一个*新建书籍*按钮，以及每本书的*编辑*和*删除*动作。如果当前用户没有授予相关权限，我们应该隐藏这些按钮/操作。

基类`AbpCrudPageBase`已经有这些操作的必需功能。

#### 设置策略（权限）名称

在`Books.razor`文件的末尾添加以下代码块：

````csharp
@code
{
    public Books() // Constructor
    {
        CreatePolicyName = BookStorePermissions.Books.Create;
        UpdatePolicyName = BookStorePermissions.Books.Edit;
        DeletePolicyName = BookStorePermissions.Books.Delete;
    }
}
````

The base `AbpCrudPageBase` class automatically checks these permissions on the related operations. It also defines the given properties for us if we need to check them manually:

基类`AbpCrudPageBase`自动检查这些相关操作的权限。如果我们需要手动检查，它也为我们定义了指定的属性。

* `HasCreatePermission`: True, 如果当前用户有权限创建该实体。
* `HasUpdatePermission`: True, 如果当前用户有权限编辑/更新该实体。
* `HasDeletePermission`: True, 如果当前用户有权限删除该实体。

> **Blazor提示**：虽然将C#代码添加到`@code`块中对于简单的代码部分是可以的，但是当代码块变长时，建议使用后台代码的方法来开发一个更可维护的代码库。我们将对作者的部分使用这种方法。

#### 隐藏新建书籍按钮

用一个 "if "块包住*新建书籍*按钮，如下所示：

````xml
@if (HasCreatePermission)
{
    <Button Color="Color.Primary"
            Clicked="OpenCreateModalAsync">@L["NewBook"]</Button>
}
````

#### 隐藏编辑/删除动作

`EntityAction` component defines `Visible` attribute (parameter) to conditionally show the action.

`EntityAction`组件定义了 `Visible`属性（参数）以条件地形式显示该操作。

更新`EntityActions`部分，如下所示：

````xml
<EntityActions TItem="BookDto" EntityActionsColumn="@EntityActionsColumn">
    <EntityAction TItem="BookDto"
                  Text="@L["Edit"]"
                  Visible=HasUpdatePermission
                  Clicked="() => OpenEditModalAsync(context)" />
    <EntityAction TItem="BookDto"
                  Text="@L["Delete"]"
                  Visible=HasDeletePermission
                  Clicked="() => DeleteEntityAsync(context)"
                  ConfirmationMessage="()=>GetDeleteConfirmationMessage(context)" />
</EntityActions>
````

#### 关于权限缓存

你可以运行并测试这些权限。从管理员角色中删除一个与书有关的权限，可以看到相关的按钮/操作从用户界面中消失。

**ABP框架在客户端缓存当前用户的权限**。因此，当你为自己改变权限时，你需要手动**刷新页面**来使其生效。如果你不刷新并试图使用被禁止的操作，你会从服务端上得到一个HTTP 403（禁止）的响应。

> 改变一个角色或用户的权限在服务端会立即生效。所以，这个缓存系统不会造成任何安全问题。

### 菜单项

即使我们已经保护了书籍管理页面的所有层面，它仍然在应用程序的主菜单上可见。如果当前用户没有权限，我们应该隐藏该菜单项。

打开`Acme.BookStore.Blazor`项目中的`BookStoreMenuContributor`类，找到下面的代码块：

````csharp
context.Menu.AddItem(
    new ApplicationMenuItem(
        "BooksStore",
        l["Menu:BookStore"],
        icon: "fa fa-book"
    ).AddItem(
        new ApplicationMenuItem(
            "BooksStore.Books",
            l["Menu:Books"],
            url: "/books"
        )
    )
);
````

并将此代码块替换为以下内容：

````csharp
var bookStoreMenu = new ApplicationMenuItem(
    "BooksStore",
    l["Menu:BookStore"],
    icon: "fa fa-book"
);

context.Menu.AddItem(bookStoreMenu);

//CHECK the PERMISSION
if (await context.IsGrantedAsync(BookStorePermissions.Books.Default))
{
    bookStoreMenu.AddItem(new ApplicationMenuItem(
        "BooksStore.Books",
        l["Menu:Books"],
        url: "/books"
    ));
}
````

你还需要在`ConfigureMenuAsync`方法上添加`async`关键字，并重组返回值。最终的`ConfigureMainMenuAsync`方法应该是这样的：

````csharp
private async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
{
    var l = context.GetLocalizer<BookStoreResource>();

    context.Menu.Items.Insert(
        0,
        new ApplicationMenuItem(
            "BookStore.Home",
            l["Menu:Home"],
            "/",
            icon: "fas fa-home"
        )
    );

    var bookStoreMenu = new ApplicationMenuItem(
        "BooksStore",
        l["Menu:BookStore"],
        icon: "fa fa-book"
    );

    context.Menu.AddItem(bookStoreMenu);

    //CHECK the PERMISSION
    if (await context.IsGrantedAsync(BookStorePermissions.Books.Default))
    {
        bookStoreMenu.AddItem(new ApplicationMenuItem(
            "BooksStore.Books",
            l["Menu:Books"],
            url: "/books"
        ));
    }
}
````

{{end}}

## 下一篇

见本教程的[下一篇](Part-6.md)。
