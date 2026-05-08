---
description: "View Rendering Skills for List Pages and Edit/Create Forms"
tags: ["views", "razor", "forms", "list-pages", "bootstrap"]
applyTo:
  - "Views/**/*.cshtml"
---

# List Page Skill

**Purpose**: Provides guidance for creating list (Index) pages in the Crypto Backtesting Dashboard that display collections of entities with filtering, sorting, and display options.

## When to Use This Skill

Use this skill when you need to:

- Create a new list/index page for an entity (e.g., `/backtests`, `/strategies`)
- Display data from the database in a table or grid format
- Add filtering, sorting, or pagination to a list view
- Add search functionality to list pages
- Create HTML views with Bootstrap styling for consistency

## Project Context

**View Location**: `CryptoBacktestingDashboard/Views/{ControllerName}/Index.cshtml`  
**Controller Method**: `Index()` action in `{ControllerName}Controller.cs`  
**Repository**: EF Repository in `Repositories/EF/{Entity}Repository.cs`  
**CSS Framework**: Bootstrap (via wwwroot/lib)

### Existing List Pages

1. `/backtests` - BacktestSession list
2. `/strategies` - BacktestStrategy list
3. `/pairs` - CryptoPair list
4. `/indicators` - Indicator list
5. `/risk` - RiskManagement list

## Workflow: Creating a New List Page

### Step 1: Ensure Controller has Index Action

```csharp
// Controllers/ExampleController.cs
[Route("examples")]
public class ExampleController : Controller
{
    private readonly ExampleRepository _repository;

    public ExampleController(ExampleRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _repository.GetItemsAsync();
        return View(items);
    }
}
```

### Step 2: Create the Index View

```html
<!-- Views/Example/Index.cshtml -->
@model IEnumerable<CryptoBacktestingDashboard.Models.Crypto.Example>
  @{ ViewData["Title"] = "Examples"; }

  <div class="container mt-4">
    <div class="row mb-4">
      <div class="col-md-8">
        <h1>@ViewData["Title"]</h1>
      </div>
      <div class="col-md-4 text-end">
        <a href="/examples/create" class="btn btn-primary">Add New</a>
      </div>
    </div>

    @if (!Model.Any()) {
    <div class="alert alert-info">No items found.</div>
    } else {
    <table class="table table-striped table-hover">
      <thead class="table-dark">
        <tr>
          <th>ID</th>
          <th>Name</th>
          <th>Status</th>
          <th>Created</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        @foreach (var item in Model) {
        <tr>
          <td>@item.Id</td>
          <td>@item.Name</td>
          <td><span class="badge bg-info">Active</span></td>
          <td>@item.CreatedAt.ToString("yyyy-MM-dd")</td>
          <td>
            <a href="/examples/@item.Id" class="btn btn-sm btn-info">View</a>
            <a href="/examples/@item.Id/edit" class="btn btn-sm btn-warning"
              >Edit</a
            >
          </td>
        </tr>
        }
      </tbody>
    </table>
    }
  </div></CryptoBacktestingDashboard.Models.Crypto.Example
>
```

## List Page Best Practices

✅ **DO**:

- Use responsive Bootstrap tables for list display
- Include action buttons (View, Edit, Delete) for each row
- Add search/filter functionality if list is large (50+ items)
- Use badges for status indicators (Active, Inactive, etc.)
- Implement pagination for large datasets
- Show "No items found" message when list is empty
- Use table-striped and table-hover for better UX

❌ **DON'T**:

- Hard-code styling (use Bootstrap classes)
- Display all properties (select important ones)
- Forget null-checking in Razor syntax
- Mix controller logic with view presentation
- Use synchronous methods in async controllers

## Related Files

- Views: `CryptoBacktestingDashboard/Views/{ControllerName}/Index.cshtml`
- Controllers: `CryptoBacktestingDashboard/Controllers/`
- Repositories: `CryptoBacktestingDashboard/Repositories/EF/`
- Styling: `CryptoBacktestingDashboard/wwwroot/lib/bootstrap/`

---

# Edit Form Skill

**Purpose**: Provides guidance for creating edit/create forms in the Crypto Backtesting Dashboard that allow users to modify or add new entities.

## When to Use This Skill

Use this skill when you need to:

- Create a form to edit an existing entity
- Create a form to add a new entity (Create)
- Add form validation and error messages
- Handle form submission and data persistence
- Create responsive HTML forms with Bootstrap

## Project Context

**View Location**: `CryptoBacktestingDashboard/Views/{ControllerName}/Edit.cshtml` or `Create.cshtml`  
**Controller Methods**: `Edit(id)` and `Edit(Model)` actions (POST)  
**Repository**: EF Repository in `Repositories/EF/{Entity}Repository.cs`  
**CSS Framework**: Bootstrap (via wwwroot/lib)

### Form Handling Pattern

ASP.NET Core uses model binding to automatically map form data to model properties.

## Workflow: Creating a New Edit/Create Form

### Step 1: Add Controller Actions

```csharp
// Controllers/ExampleController.cs
[Route("examples")]
public class ExampleController : Controller
{
    private readonly ExampleRepository _repository;

    public ExampleController(ExampleRepository repository)
    {
        _repository = repository;
    }

    // GET: examples/create
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Example());
    }

    // POST: examples/create
    [HttpPost("create")]
    public async Task<IActionResult> Create(Example model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _repository.AddItemAsync(model);
        return RedirectToAction("Index");
    }

    // GET: examples/{id}/edit
    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _repository.GetItemAsync(id);
        if (item == null)
            return NotFound();

        return View(item);
    }

    // POST: examples/{id}/edit
    [HttpPost("{id}/edit")]
    public async Task<IActionResult> Edit(int id, Example model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        await _repository.UpdateItemAsync(model);
        return RedirectToAction("Details", new { id = model.Id });
    }
}
```

### Step 2: Add Repository Methods (if not already present)

```csharp
// Repositories/EF/ExampleRepository.cs
public async Task<Example> AddItemAsync(Example item)
{
    _context.Examples.Add(item);
    await _context.SaveChangesAsync();
    return item;
}

public async Task UpdateItemAsync(Example item)
{
    _context.Examples.Update(item);
    await _context.SaveChangesAsync();
}
```

### Step 3: Create the Edit/Create View

```html
<!-- Views/Example/Edit.cshtml or Create.cshtml -->
@model CryptoBacktestingDashboard.Models.Crypto.Example @{ ViewData["Title"] =
Model.Id == 0 ? "Create Example" : "Edit Example"; }

<div class="container mt-4">
  <div class="row">
    <div class="col-md-8 offset-md-2">
      <h1>@ViewData["Title"]</h1>

      <form method="post" class="mt-4">
        @if (Model.Id != 0) {
        <input type="hidden" asp-for="Id" />
        }

        <!-- Name Field -->
        <div class="mb-3">
          <label asp-for="Name" class="form-label">Name</label>
          <input asp-for="Name" class="form-control" placeholder="Enter name" />
          <span asp-validation-for="Name" class="text-danger"></span>
        </div>

        <!-- Description Field -->
        <div class="mb-3">
          <label asp-for="Description" class="form-label">Description</label>
          <textarea
            asp-for="Description"
            class="form-control"
            rows="3"
            placeholder="Enter description"
          ></textarea>
          <span asp-validation-for="Description" class="text-danger"></span>
        </div>

        <!-- Status Toggle -->
        <div class="mb-3">
          <div class="form-check">
            <input
              asp-for="IsActive"
              type="checkbox"
              class="form-check-input"
            />
            <label asp-for="IsActive" class="form-check-label">Active</label>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="mt-4">
          <button type="submit" class="btn btn-primary">Save</button>
          <a href="/examples" class="btn btn-secondary">Cancel</a>
        </div>
      </form>

      @section Scripts { @{ await
      Html.RenderPartialAsync("_ValidationScriptsPartial"); } }
    </div>
  </div>
</div>
```

## Form Best Practices

✅ **DO**:

- Use ASP.NET Tag Helpers (`asp-for`, `asp-validation-for`, etc.)
- Validate data on both client and server
- Show validation error messages to users
- Use responsive Bootstrap grid layout
- Provide clear form labels for accessibility
- Include Cancel button to return to list
- Use checkboxes for boolean properties
- Use appropriate input types (email, date, number, etc.)

❌ **DON'T**:

- Mix validation logic in controller and model
- Forget to validate ModelState on POST
- Display all properties in the form (only editable ones)
- Use raw HTML form tags instead of Tag Helpers
- Leave required fields without validation
- Use GET for form submissions that modify data
- Display sensitive data in forms

## ASP.NET Tag Helper Reference

| Tag Helper           | Purpose                       | Example                                            |
| -------------------- | ----------------------------- | -------------------------------------------------- |
| `asp-for`            | Binds input to model property | `<input asp-for="Name" />`                         |
| `asp-validation-for` | Shows validation errors       | `<span asp-validation-for="Name"></span>`          |
| `asp-action`         | Links to controller action    | `<a asp-action="Details" asp-route-id="@item.Id">` |
| `asp-controller`     | Specifies target controller   | `<a asp-controller="Example" asp-action="Index">`  |
| `asp-route-{param}`  | Adds route parameter          | `asp-route-id="@item.Id"`                          |

## Related Files

- Views: `CryptoBacktestingDashboard/Views/{ControllerName}/Edit.cshtml`
- Controllers: `CryptoBacktestingDashboard/Controllers/`
- Repositories: `CryptoBacktestingDashboard/Repositories/EF/`
- Models: `CryptoBacktestingDashboard/Models/Crypto/`
- Styling: `CryptoBacktestingDashboard/wwwroot/lib/bootstrap/`
