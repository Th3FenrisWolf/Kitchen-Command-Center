import React, { useCallback, useEffect, useRef } from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import { Button, ButtonColor } from "@kentico/xperience-admin-components";

const HIDDEN_SUBMIT_LABEL = "__submit__";

function findHiddenSubmitButton(): HTMLButtonElement | null {
  for (const btn of document.querySelectorAll("button")) {
    if (btn.textContent?.trim() === HIDDEN_SUBMIT_LABEL) {
      return btn as HTMLButtonElement;
    }
  }
  return null;
}

interface SaveAndAddAnotherProps extends FormComponentProps {
  showAddAnother?: boolean;
}

export const SaveAndAddAnotherFormComponent = (
  props: SaveAndAddAnotherProps
) => {
  const hiddenBtnRef = useRef<HTMLButtonElement | null>(null);

  useEffect(() => {
    const style = document.createElement("style");
    style.textContent = `
      div[class*="actions-wrapper"], div[class*="sticky__"] { display: none !important; }
      div[class*="column__"] { padding-top: 0 !important; }
      div[class*="column__"] > div:first-child { padding-top: 0 !important; margin-top: 0 !important; }
    `;
    document.head.appendChild(style);

    const hide = () => {
      const btn = findHiddenSubmitButton();
      if (btn) {
        hiddenBtnRef.current = btn;
        const wrapper = btn.closest("[class]")?.parentElement;
        if (wrapper) {
          wrapper.style.setProperty("display", "none", "important");
        }
      }
    };

    hide();
    const timer = setTimeout(hide, 500);
    return () => {
      clearTimeout(timer);
      style.remove();
    };
  }, []);

  const triggerSubmit = useCallback((addAnother: boolean) => {
    props.onChange?.(addAnother);

    setTimeout(() => {
      const btn = hiddenBtnRef.current ?? findHiddenSubmitButton();
      btn?.click();
    }, 50);
  }, [props.onChange]);

  return (
    <div style={{ display: "flex", gap: "8px" }}>
      <Button
        label="Save"
        color={ButtonColor.Primary}
        onClick={() => triggerSubmit(false)}
      />
      {props.showAddAnother !== false && (
        <Button
          label="Save & Add Another"
          color={ButtonColor.Secondary}
          onClick={() => triggerSubmit(true)}
        />
      )}
    </div>
  );
};
