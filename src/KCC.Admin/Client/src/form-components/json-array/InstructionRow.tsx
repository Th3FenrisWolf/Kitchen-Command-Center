import React from "react";
import { Instruction } from "./types";

interface InstructionRowProps {
  item: Instruction;
  index: number;
  onChange: (next: Instruction) => void;
  disabled: boolean;
  invalid: boolean;
}

export const InstructionRow = ({ item, index, onChange, disabled, invalid }: InstructionRowProps) => (
  <div className={"kcc-instruction-row" + (invalid ? " is-invalid" : "")}>
    <span className="kcc-instruction-row__step">{index + 1}</span>
    <textarea
      className="kcc-instruction-row__text"
      rows={2}
      aria-label={`Step ${index + 1}`}
      placeholder="Step description"
      value={item.text}
      disabled={disabled}
      onChange={(e) => onChange({ ...item, text: e.target.value })}
    />
  </div>
);
