import React, { useMemo, useState } from "react";
import { FormComponentProps, useFormComponentCommandProvider } from "@kentico/xperience-admin-base";
import {
  Button,
  ButtonColor,
  ButtonSize,
  ButtonType,
  FormEditMode,
  FormItemWrapper,
  Input,
} from "@kentico/xperience-admin-components";
import "@fortawesome/fontawesome-pro/css/fontawesome.css";
import "@fortawesome/fontawesome-pro/css/solid.css";
import "@fortawesome/fontawesome-pro/css/duotone.css";
import COMPONENT_CSS from "./RecipeIconSelectorFormComponent.css";

interface RecipeIconSelectorProps extends FormComponentProps<string> {
  icons: string[];
}

/** Matches the [FormComponentCommand] handler name on RecipeIconSelectorFormComponent. */
const SUGGEST_ICON_COMMAND = "SuggestIcon";

interface SuggestIconArguments {
  name: string;
  description: string;
}

interface SuggestIconResponse {
  icon: string;
}

export const RecipeIconSelectorFormComponent = (props: RecipeIconSelectorProps) => {
  const { value, onChange, icons, editMode, fieldValues } = props;
  const { executeCommand } = useFormComponentCommandProvider();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState(value ?? "");
  const [suggesting, setSuggesting] = useState(false);

  const suggestIcon = async () => {
    setSuggesting(true);
    try {
      const result = await executeCommand<SuggestIconResponse, SuggestIconArguments>(
        props,
        SUGGEST_ICON_COMMAND,
        {
          name: (fieldValues?.["Name"] as string) ?? "",
          description: (fieldValues?.["Description"] as string) ?? "",
        },
      );
      if (result?.icon) {
        onChange?.(result.icon);
      }
    } finally {
      setSuggesting(false);
    }
  };

  const filtered = useMemo(
    () => (icons ?? []).filter((icon) => icon.toLowerCase().includes(search.toLowerCase())),
    [icons, search],
  );

  const labelFor = (icon: string) => icon.replace(/^fa-\w+\s+fa-/, "");

  const isReadOnly = editMode === FormEditMode.ReadOnly;
  const isDisabled = editMode === FormEditMode.Disabled;

  const preview = (
    <div className="kcc-icon-selector__preview-row">
      {value ? (
        <>
          <span className="kcc-icon-selector__preview">
            <i className={value} />
          </span>
          <span className="kcc-icon-selector__value">{labelFor(value)}</span>
        </>
      ) : (
        <span className="kcc-icon-selector__empty">-</span>
      )}
    </div>
  );

  return (
    <FormItemWrapper
      label={props.label}
      explanationText={props.explanationText}
      invalid={props.invalid}
      validationMessage={props.validationMessage}
      markAsRequired={props.required}
      labelIcon={props.tooltip ? "xp-i-circle" : undefined}
      labelIconTooltip={props.tooltip}
      editMode={editMode}
    >
      <style>{COMPONENT_CSS}</style>

      {isReadOnly ? (
        preview
      ) : (
        <div className="kcc-icon-selector__field">
          <div className="kcc-icon-selector__actions">
            <Button
              type={ButtonType.Button}
              size={ButtonSize.S}
              color={ButtonColor.Secondary}
              label="Select Icon"
              disabled={isDisabled}
              onClick={() => {
                setSelected(value ?? "");
                setOpen(true);
              }}
            />
            <span className="kcc-icon-selector__or">or</span>
            <Button
              type={ButtonType.Button}
              size={ButtonSize.S}
              color={ButtonColor.Secondary}
              trailingIcon="xp-ai"
              label={suggesting ? "Suggesting…" : "Suggest with AI"}
              disabled={isDisabled || suggesting}
              onClick={suggestIcon}
            />
          </div>
          {value && preview}
        </div>
      )}

      {open && !isReadOnly && !isDisabled && (
        <div className="kcc-icon-modal" role="dialog" aria-modal="true">
          <div className="kcc-icon-modal__backdrop" onClick={() => setOpen(false)} />
          <div className="kcc-icon-modal__panel">
            <div className="kcc-icon-modal__header">
              <h2>Select an icon</h2>
            </div>
            <div className="kcc-icon-modal__search">
              <Input
                placeholder="Search icons"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="kcc-icon-modal__grid">
              {filtered.map((icon) => (
                <button
                  type="button"
                  key={icon}
                  title={icon}
                  className={
                    "kcc-icon-modal__item" +
                    (selected === icon ? " kcc-icon-modal__item--selected" : "")
                  }
                  onClick={() => setSelected(icon)}
                >
                  <i className={icon} />
                  <span className="kcc-icon-modal__name">{labelFor(icon)}</span>
                </button>
              ))}
            </div>
            <div className="kcc-icon-modal__footer">
              <Button
                type={ButtonType.Button}
                size={ButtonSize.M}
                color={ButtonColor.Secondary}
                label="Cancel"
                onClick={() => setOpen(false)}
              />
              <Button
                type={ButtonType.Button}
                size={ButtonSize.M}
                color={ButtonColor.Primary}
                label="Select Icon"
                disabled={!selected}
                onClick={() => {
                  onChange?.(selected);
                  setOpen(false);
                }}
              />
            </div>
          </div>
        </div>
      )}
    </FormItemWrapper>
  );
};
