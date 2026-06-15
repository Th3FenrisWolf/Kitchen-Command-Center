import React, { useEffect, useMemo, useRef, useState } from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import { FormEditMode, FormItemWrapper } from "@kentico/xperience-admin-components";
import { moveItem, parseItems, reconcileIncomingValue, serializeItems } from "./helpers";
import CSS from "./json-array.css";

/** Context handed to a row renderer. */
export interface RowContext {
  disabled: boolean;
  invalid: boolean;
  index: number;
}

export interface JsonArrayEditorProps<T> extends FormComponentProps<string> {
  /** Factory for a new, empty item. */
  newItem: () => T;
  /** Render the editable fields for one item (no handle/remove — the core supplies those). */
  renderRow: (item: T, onChange: (next: T) => void, ctx: RowContext) => React.ReactNode;
  /** Render the read-only summary content for one item (the core wraps it in <li>). */
  renderSummary: (item: T, index: number) => React.ReactNode;
  /** True when the item satisfies its required field(s). */
  isItemValid: (item: T) => boolean;
  /** Normalize items just before serialization (e.g. clear eyeballed measures, stamp steps). */
  prepareForSave?: (items: T[]) => T[];
  /** List element for the read-only summary. */
  summaryListTag: "ul" | "ol";
  addLabel: string;
  emptyText: string;
  /** Message shown when at least one row is missing its required field. */
  invalidMessage: string;
  /** Message shown when the list is empty (the field requires at least one row). */
  emptyInvalidMessage: string;
}

export function JsonArrayEditor<T>(props: JsonArrayEditorProps<T>) {
  const {
    value,
    onChange,
    editMode,
    newItem,
    renderRow,
    renderSummary,
    isItemValid,
    prepareForSave,
    summaryListTag,
    addLabel,
    emptyText,
    invalidMessage,
    emptyInvalidMessage,
  } = props;

  // Seed local state from the initial value once. Kentico owns the field value and can replace
  // it out from under us (revert to published, undo, version restore, language switch); the
  // effect below re-hydrates when that happens. The echo of our own onChange is ignored so
  // in-progress edits stay responsive and don't flicker.
  const initial = useMemo(() => parseItems<T>(value), []); // eslint-disable-line react-hooks/exhaustive-deps
  const [items, setItems] = useState<T[]>(initial.items);
  const [parseFailed, setParseFailed] = useState(initial.error);
  const lastEmitted = useRef<string | null | undefined>(value);

  useEffect(() => {
    const reconciled = reconcileIncomingValue<T>(value, lastEmitted.current);
    if (reconciled) {
      setItems(reconciled.items);
      setParseFailed(reconciled.error);
      lastEmitted.current = value;
    }
  }, [value]);

  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [overIndex, setOverIndex] = useState<number | null>(null);
  const [armed, setArmed] = useState(false);

  const isReadOnly = editMode === FormEditMode.ReadOnly;
  const disabled = editMode === FormEditMode.Disabled;

  const prepare = prepareForSave ?? ((x: T[]) => x);

  const update = (next: T[]) => {
    setItems(next);
    const serialized = serializeItems(prepare(next));
    lastEmitted.current = serialized; // remember our own emission so the effect doesn't treat the echo as external
    onChange?.(serialized);
  };

  const changeItem = (index: number, next: T) => update(items.map((it, i) => (i === index ? next : it)));
  const removeItem = (index: number) => update(items.filter((_, i) => i !== index));
  const addItem = () => update([...items, newItem()]);

  const resetDrag = () => {
    setDragIndex(null);
    setOverIndex(null);
    setArmed(false);
  };

  const invalidFlags = items.map((it) => !isItemValid(it));
  const hasInvalidItem = invalidFlags.some(Boolean);
  const isEmptyList = items.length === 0 && !isReadOnly && !parseFailed;
  const invalid = props.invalid || hasInvalidItem || isEmptyList;
  const validationMessage =
    props.validationMessage ||
    (hasInvalidItem ? invalidMessage : isEmptyList ? emptyInvalidMessage : undefined);

  const Tag = summaryListTag;

  let content: React.ReactNode;

  if (parseFailed) {
    content = (
      <div className="kcc-json-array__error">
        <p>This field contains data that is not valid JSON and cannot be shown in the editor. The raw value is preserved below.</p>
        <pre>{value}</pre>
        {!isReadOnly && (
          <button
            type="button"
            className="kcc-json-array__add"
            onClick={() => {
              setParseFailed(false);
              update([]);
            }}
          >
            Start fresh (clears this field)
          </button>
        )}
      </div>
    );
  } else if (isReadOnly) {
    content =
      items.length === 0 ? (
        <p className="kcc-json-array__empty">{emptyText}</p>
      ) : (
        <Tag className="kcc-json-array__summary">
          {items.map((it, i) => (
            <li key={i}>{renderSummary(it, i)}</li>
          ))}
        </Tag>
      );
  } else {
    content = (
      <div className="kcc-json-array">
        {items.map((it, i) => (
          <div
            key={i}
            className={
              "kcc-json-array__row" +
              (invalidFlags[i] ? " is-invalid" : "") +
              (overIndex === i && dragIndex !== null && dragIndex !== i ? " is-drop-target" : "")
            }
            draggable={armed && !disabled}
            onDragStart={(e) => {
              if (!armed) {
                e.preventDefault();
                return;
              }
              setDragIndex(i);
              e.dataTransfer.effectAllowed = "move";
            }}
            onDragOver={(e) => {
              if (dragIndex === null) {
                return;
              }
              e.preventDefault();
              setOverIndex(i);
              e.dataTransfer.dropEffect = "move";
            }}
            onDrop={(e) => {
              e.preventDefault();
              if (dragIndex !== null) {
                update(moveItem(items, dragIndex, i));
              }
              resetDrag();
            }}
            onDragEnd={resetDrag}
          >
            <span
              className="kcc-json-array__handle"
              aria-label="Drag to reorder"
              onMouseDown={() => {
                if (!disabled) {
                  setArmed(true);
                }
              }}
              onMouseUp={() => setArmed(false)}
            >
              ⠿
            </span>
            <div className="kcc-json-array__fields">
              {renderRow(it, (next) => changeItem(i, next), { disabled, invalid: invalidFlags[i], index: i })}
            </div>
            <button
              type="button"
              className="kcc-json-array__remove"
              aria-label="Remove"
              disabled={disabled}
              onClick={() => removeItem(i)}
            >
              ✕
            </button>
          </div>
        ))}
        <button type="button" className="kcc-json-array__add" disabled={disabled} onClick={addItem}>
          + {addLabel}
        </button>
      </div>
    );
  }

  return (
    <FormItemWrapper
      label={props.label}
      explanationText={props.explanationText}
      invalid={invalid}
      validationMessage={validationMessage}
      markAsRequired={props.required}
      labelIcon={props.tooltip ? "xp-i-circle" : undefined}
      labelIconTooltip={props.tooltip}
      editMode={editMode}
    >
      <style>{CSS}</style>
      {content}
    </FormItemWrapper>
  );
}
