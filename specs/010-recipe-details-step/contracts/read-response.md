# Contract: The read with Details

`POST /api/recipe-imports/read` keeps its request, limits, statuses and spending rules
([001 read-endpoint.md](../../001-photo-recipe-import/contracts/read-endpoint.md)). Its prompt gains the site's lists,
its reader output gains `details`, and its `recipe` outcome gains `details`.

## What the service is sent (FR-020)

Besides Phase 1's photos, instruction and catalog, the instruction carries the lists, one line each, after the
catalog:

```text
The site's categories follow, one a line, each with its id and name. Choose at most one:
{"id":"C1","name":"Breakfast"}
…
The site's tags follow, one a line, each with its id, name and group:
{"id":"T1","name":"Vegetarian","group":"diet"}
…
```

On the variant flow the categories are left out and the instruction says to set `details.category` to null. No
member account data is sent. `ReadCost.ReservationMicroUsd` counts these characters with the catalog's (spec A10).

## Prompt rules added

10. Choose a category and tags only from the lists by id. Set `stated` true only when the photos name that category or
    tag, or an equivalent word ("plant-based" for Vegan); copy the word the photos use into `printed`. When several
    categories fit, choose one and name the one passed over in the reason. When none fits, set the category to null.
11. Choose a diet tag only when the photos state it. When the recipe contradicts a stated diet tag, keep the tag and
    mark it unclear, naming the ingredient. A style tag may be chosen from the recipe itself, with a reason.
12. Set the difficulty from a printed level when there is one, copying the printed word into `printed`; otherwise
    judge it and give a reason. Difficulty and the Easy tag are separate decisions.
13. Copy nutrition only as printed. Never estimate a value. Give each printed nutrient once, with its amount, unit and
    printed text, and set `basis` to serving or recipe as the panel says, or to null when it does not say. A range or a
    "less than" keeps its lower number, `<1` as 0, and sets `inexact`. Leave out nutrients the list does not name.

## Reader output schema additions

`details` joins the root's `required` list:

```json
"details": {
  "type": "object", "additionalProperties": false,
  "required": ["category", "tags", "difficulty", "nutrition"],
  "properties": {
    "category": {
      "type": "object", "additionalProperties": false, "required": ["id", "stated", "printed", "reason"],
      "properties": {
        "id": { "type": ["string", "null"] },
        "stated": { "type": "boolean" },
        "printed": { "type": ["string", "null"] },
        "reason": { "type": ["string", "null"] }
      }
    },
    "tags": {
      "type": "array",
      "items": {
        "type": "object", "additionalProperties": false, "required": ["id", "stated", "printed", "mark"],
        "properties": {
          "id": { "type": "string" },
          "stated": { "type": "boolean" },
          "printed": { "type": ["string", "null"] },
          "mark": { "$ref": "#/$defs/mark" }
        }
      }
    },
    "difficulty": {
      "type": "object", "additionalProperties": false, "required": ["level", "printed", "reason"],
      "properties": {
        "level": { "type": ["string", "null"], "enum": ["Easy", "Medium", "Hard", null] },
        "printed": { "type": ["string", "null"] },
        "reason": { "type": ["string", "null"] }
      }
    },
    "nutrition": {
      "type": "object", "additionalProperties": false, "required": ["basis", "values"],
      "properties": {
        "basis": { "type": ["string", "null"], "enum": ["serving", "recipe", null] },
        "values": {
          "type": "array",
          "items": {
            "type": "object", "additionalProperties": false,
            "required": ["nutrient", "amount", "unit", "printed", "inexact"],
            "properties": {
              "nutrient": { "type": "string", "enum": ["calories", "protein", "carbohydrates", "fat", "saturatedFat", "fiber", "sugar", "sodium", "salt"] },
              "amount": { "type": ["number", "null"] },
              "unit": { "type": "string", "enum": ["kcal", "kJ", "g", "mg"] },
              "printed": { "type": "string" },
              "inexact": { "type": "boolean" }
            }
          }
        }
      }
    }
  }
}
```

The contract copy in `specs/001-photo-recipe-import/contracts/reader-output.schema.json` is Phase 1's record and does
not change. `ReaderOutputSchema.json` is the live copy. `--check` (free) proves the service takes it before any paid
run.

## Response additions

```json
"details": {
  "category": { "value": "f08799ce-…", "mark": { "kind": "suggested", "reason": "A main dish; Lunch also fits", "code": null, "arg": null } },
  "tags": [
    { "key": "b8563daf-…", "mark": null },
    { "key": "fefc672b-…", "mark": { "kind": "suggested", "reason": "Two chillies and cayenne", "code": null, "arg": null } }
  ],
  "difficulty": { "value": "Medium", "mark": { "kind": "suggested", "reason": null, "code": "printedAs", "arg": "intermediate" } },
  "nutrition": {
    "calories": { "value": 287, "mark": { "kind": "calculated", "reason": null, "code": "printedAs", "arg": "1200 kJ" } },
    "proteinG": { "value": 12, "mark": null },
    "sodiumMg": { "value": null, "mark": null }
  }
}
```

All eight nutrition keys are always present, and a panel whose basis is null leaves every one empty and unmarked. A
category or tag the photos name by an equivalent word keeps its value and carries `suggested` with code `printedAs` and
the printed word as `arg`. The rules that produce these values are research R3 and R4. A `noRecipe`
outcome carries the same shape with every value null and no tags.
