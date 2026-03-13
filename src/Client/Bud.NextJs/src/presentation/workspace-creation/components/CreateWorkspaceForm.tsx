"use client";

import { Form, Formik } from "formik";
import { CreateWorkspaceSchema } from "../schemas/create-workspace-schema";
import { WorkspaceVisibility } from "@/types/workspace/WorkspaceVisibilityEnum";
import { TextField } from "@/components/form-values/TextFieldComponent";
import { Button } from "@/components/ui/button";
import { SelectField } from "@/components/form-values/SelectFieldComponent";
import { FileField } from "@/components/form-values/FileFieldComponent";

export function CreateWorkspaceForm() {
  const options = Object.entries(WorkspaceVisibility).map((i) => {
    return { label: i[1], value: i[0] };
  });
  return (
    <Formik
      initialValues={{
        file: "",
        name: "",
        visibility: "",
      }}
      validationSchema={CreateWorkspaceSchema}
      onSubmit={(values) => {
        console.log(values);
      }}
    >
      <Form className="flex flex-col">
        <FileField label="Ícone" name="icon" accept="image/jpg,image/png" />

        <TextField
          label="Nome do espaço de trabalho"
          name="name"
          placeholder="Nome do espaço de trabalho"
        />

        <SelectField label="Visibilidade" name="visibility" options={options} />

        <Button variant="outline" type="submit">
          Criar
        </Button>
      </Form>
    </Formik>
  );
}
