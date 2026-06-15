import React from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import { JsonArrayEditor } from "./JsonArrayEditor";
import { InstructionRow } from "./InstructionRow";
import { Instruction } from "./types";
import { isInstructionValid, stampSteps } from "./helpers";
import INSTRUCTION_CSS from "./instruction-row.css";

export const InstructionsEditorFormComponent = (props: FormComponentProps<string>) => (
  <>
    <style>{INSTRUCTION_CSS}</style>
    <JsonArrayEditor<Instruction>
      {...props}
      newItem={() => ({ step: 0, text: "" })}
      renderRow={(item, onChange, ctx) => (
        <InstructionRow item={item} index={ctx.index} onChange={onChange} disabled={ctx.disabled} invalid={ctx.invalid} />
      )}
      renderSummary={(item) => item.text}
      isItemValid={isInstructionValid}
      prepareForSave={stampSteps}
      summaryListTag="ol"
      addLabel="Add step"
      emptyText="No steps yet."
      invalidMessage="Every step needs a description."
      emptyInvalidMessage="Add at least one step."
    />
  </>
);
