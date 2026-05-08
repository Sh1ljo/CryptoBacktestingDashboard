---
name: edit-form
description: Creates edit/create forms for ASP.NET MVC entities
applyTo:
  - "**/*.cshtml"
  - "**/Controllers/**/*.cs"
---

# Edit Form Skill

This skill helps you create and manage edit/create form pages for ASP.NET MVC applications in the Crypto Backtesting Dashboard project.

## When to Use This Skill

Use this skill when you need to:
- Create a new edit form (cshtml view) for an entity
- Create a new create form (cshtml view) for an entity
- Add Edit/Create action methods to a controller
- Add Update/Create POST handlers to a controller
- Implement form validation
- Handle form submissions with proper error handling

## Typical Workflow

### 1. **Controller Actions**

Create both GET and POST action methods:

```csharp
// GET - Display empty form for creating new entity
[HttpGet("create")]
public IActionResult Create()
{
    return View(new EntityModel());
}

// POST - Handle form submission for creating
[HttpPost("create")]
public async Task<IActionResult> Create(EntityModel model)
{
    if (!ModelState.IsValid)
        return View(model);
    
    await _repository.AddAsync(model);
    return RedirectToAction("Index");
}

// GET - Display form with existing data for editing
[HttpGet("{id}/edit")]
public async Task<IActionResult> Edit(int id)
{
    var entity = await _repository.GetItemAsync(id);
    if (entity == null)
        return NotFound();
    
    return View(entity);
}

// POST - Handle form submission for updating
[HttpPost("{id}/edit")]
public async Task<IActionResult> Edit(int id, EntityModel model)
{
    if (id != model.Id)
        return BadRequest();
    
    if (!ModelState.IsValid)
        return View(model);
    
    await _repository.UpdateAsync(model);
    return RedirectToAction("Details", new { id = id });
}
```

### 2. **Razor View (cshtml)**

Use tag helpers for form generation:

```html
@model YourNamespace.Models.EntityModel

@{
    ViewData["Title"] = Model.Id == 0 ? "Create Entity" : "Edit Entity";
}

<div class="container">
    <h2>@ViewData["Title"]</h2>
    
    <form asp-action="@(Model.Id == 0 ? "Create" : "Edit")" method="post" class="form-container">
        @if (Model.Id > 0)
        {
            <input type="hidden" asp-for="Id" />
        }
        
        <div class="form-group">
            <label asp-for="Name"></label>
            <input type="text" asp-for="Name" class="form-control" placeholder="Enter name" />
            <span asp-validation-for="Name" class="text-danger"></span>
        </div>
        
        <div class="form-group">
            <label asp-for="Description"></label>
            <textarea asp-for="Description" class="form-control" rows="4"></textarea>
            <span asp-validation-for="Description" class="text-danger"></span>
        </div>
        
        <div class="form-actions">
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Cancel</a>
        </div>
    </form>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

### 3. **Partial View for Reusable Form**

Create `_EntityForm.cshtml` partial:

```html
@model YourNamespace.Models.EntityModel

<div class="form-group">
    <label asp-for="Name"></label>
    <input type="text" asp-for="Name" class="form-control" />
    <span asp-validation-for="Name" class="text-danger"></span>
</div>

<div class="form-group">
    <label asp-for="Description"></label>
    <textarea asp-for="Description" class="form-control" rows="4"></textarea>
    <span asp-validation-for="Description" class="text-danger"></span>
</div>
```

Then use it in both Create.cshtml and Edit.cshtml:

```html
<form asp-action="@(Model.Id == 0 ? "Create" : "Edit")" method="post">
    @if (Model.Id > 0)
    {
        <input type="hidden" asp-for="Id" />
    }
    
    <partial name="_EntityForm" model="Model" />
    
    <button type="submit" class="btn btn-primary">Save</button>
    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
</form>
```

## Key Points

- **Validation**: Always check `ModelState.IsValid` before saving
- **IDs**: Use hidden inputs for IDs when editing
- **Error Handling**: Return the form view if validation fails so users can see errors
- **Redirects**: After successful save, redirect to Details or Index (not the form itself)
- **Partial Views**: Use partials to avoid code duplication between Create and Edit forms
- **Tag Helpers**: Use `asp-for`, `asp-action`, `asp-validation-for` for proper model binding
- **Hidden Fields**: Include the ID as a hidden field when editing to maintain referential integrity

## Example: Adding Edit Form to Indicator Entity

When asked to create an edit form for Indicators:

1. Add controller actions for Create and Edit (GET/POST)
2. Create `Views/Indicator/Create.cshtml`
3. Create `Views/Indicator/Edit.cshtml` (or shared `_IndicatorForm.cshtml`)
4. Add appropriate routing: `[HttpGet("create")]`, `[HttpPost("create")]`, `[HttpGet("{id}/edit")]`, `[HttpPost("{id}/edit")]`
5. Implement validation in the model using `[Required]`, `[StringLength]`, etc.
