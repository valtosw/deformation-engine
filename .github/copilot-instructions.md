# Deformation Engine - Copilot Instructions & Code Style Guide

This document dictates the coding conventions and style rules for the `Deformation Engine` codebase. When generating, refactoring, or modifying code, you must strictly adhere to the following guidelines.

## 1. Mandatory Requirements
- **Always use `var`**: Use implicit typing (`var`) for local variables universally, unless an explicit type is strictly required by the compiler (e.g., when the type cannot be inferred or when handling specific numeric conversions). 
- **Allman Style Braces**: Always place opening and closing curly braces `{` and `}` on their own dedicated lines. This applies to namespaces, classes, methods, `if`/`else` statements, loops, etc. **No K&R style / Egyptian brackets.**
- **No Expression-Bodied Methods**: Do not define functions inline using the expression body syntax (`=>`). Always use block bodies `{ ... }` for methods. *(Note: Expression-bodied properties are acceptable and encouraged where appropriate).*
- **No Abbreviations**: Write out full, descriptive words for all identifiers (variables, fields, properties, classes, methods). Do not abbreviate unless the abbreviation is an industry-wide standard (e.g., `Id`, `UI`, `UV`, `IO`). For example, use `position` instead of `pos`, `direction` instead of `dir`, `index` instead of `i` (except in standard for-loops).

## 2. Naming Conventions
- **Classes, Records, Interfaces, and Structs**: Use `PascalCase`. Prefix interfaces with an uppercase `I` (e.g., `ICameraSystem`, `ISceneRenderer`).
- **Methods and Properties**: Use `PascalCase` (e.g., `ProcessInput`, `WorldTransform`).
- **Fields**: Private fields must use `camelCase` prefixed with an underscore `_` (e.g., `private int _viewportWidth;`).
- **Local Variables and Parameters**: Use `camelCase` (e.g., `mousePosition`, `renderingContext`).
- **Constants**: Use `PascalCase` for standard constants. Group them into static classes when possible (e.g., `MathConstants.LengthTolerance`).

## 3. Class Structure & Regions
Classes must be highly organized using C# `#region` directives to separate members logically. Keep a clean order of definitions:
1. `#region Fields` (Private/Internal fields)
2. `#region Constructors` 
3. `#region Properties`
4. `#region Public Logic` (Public methods)
5. `#region Private Logic` (Private helper methods)

*Example:*
```csharp
public sealed class ExampleClass
{
    #region Fields
    
    private readonly int _targetNode;
    
    #endregion
    
    #region Constructors
    
    public ExampleClass()
    {
        // Initialization
    }
    
    #endregion
    
    #region Public Logic
    
    public void Execute()
    {
        // Block body method
    }
    
    #endregion
}
```

## 4. Object-Oriented Principles
- **Sealed by Default**: Mark classes as `sealed` by default (`public sealed class X`) unless they are specifically designed to be inherited from (like `SceneNode`).
- **Primary Constructors**: Utilize C# 12 primary constructors for dependency injection or simple data containers where it removes boilerplate, but respect region blocks if additional traditional constructors are required.
- **Records for Data**: Use `record` or `readonly record struct` for immutable data objects, DTOs, and event payloads (e.g., `public readonly record struct MouseClickEvent(...)`).

## 5. Modern C# Language Features
- **Collection Expressions**: Use the modern collection initialization syntax `[]` for empty arrays/lists or when initializing collections (e.g., `private readonly List<IController> _controllers = [];`).
- **Target-typed `new`**: Use `new()` when the type is already known from the declaration (e.g., `private readonly SceneNode _targetNode = new();`).
- **Switch Expressions**: Prefer `switch` expressions over `switch` statements for mapping and assignments, but remember the method containing the `switch` expression must have a block body.
- **Pattern Matching**: Utilize modern pattern matching (e.g., `if (e is not KeyEvent { InputType: InputType.Down } keyEvent)`).
- **Null Safety**: The project uses `<Nullable>enable</Nullable>`. Handle nullability properly. Use `is null` and `is not null` instead of `== null` and `!= null`.
- **Local Functions**: Use local functions inside methods to encapsulate repeated private logic without polluting the class scope.

## 6. Code Formatting Example
Below is an example of a class conforming entirely to this project's style instructions:

```csharp
using System;
using Deformation.Interaction.Abstractions;

namespace Deformation.Example
{
    public sealed class InputProcessor : IInputProcessor
    {
        #region Fields

        private readonly ICameraSystem _cameraSystem;
        private bool _isProcessing;

        #endregion

        #region Constructors

        public InputProcessor(ICameraSystem cameraSystem)
        {
            _cameraSystem = cameraSystem;
            _isProcessing = false;
        }

        #endregion

        #region Public Logic

        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (inputEvent is null)
            {
                return false;
            }

            var result = EvaluateEvent(inputEvent);
            
            return result;
        }

        #endregion

        #region Private Logic

        private bool EvaluateEvent(IInputEvent inputEvent)
        {
            // Always block-bodied methods, no expression-bodied definitions.
            return inputEvent switch
            {
                MouseClickEvent mouseClickEvent => HandleClick(mouseClickEvent),
                _                               => false
            };
        }
        
        private bool HandleClick(MouseClickEvent mouseClickEvent)
        {
            var isLeftClick = mouseClickEvent.Button == MouseButton.Left;
            
            if (isLeftClick)
            {
                _cameraSystem.ZoomToFit();
            }
            
            return isLeftClick;
        }

        #endregion
    }
}
```
```
```
