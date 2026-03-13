"use client";

import Link from "next/link";

import { TextField } from "@/components/form-values/TextFieldComponent";
import { Button } from "@/components/ui/button";
import { useWorkspace } from "@/providers/workspace-provider";
import { useUser } from "@auth0/nextjs-auth0";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Form, Formik } from "formik";
import { redirect } from "next/navigation";

interface UserInviteDto {
  FromEmail: string;
  Email: string;
  Role: string;
}

export default function Home() {
  const { user } = useUser();
  const { currentWorkspace, setWorkspace } = useWorkspace();

  console.log(currentWorkspace);
  if (!currentWorkspace) {
    redirect("/workspace");
  }

  /*
  const { data: usuario } = useQuery({
    queryKey: ["meu-perfil"],
    queryFn: async () => {
      const res = await fetch("/api/user/get-user");
      return res.json();
    },
  });
  */

  const exitWorkspace = () => {
    setWorkspace(undefined);
  };

  const createInviteMutation = useMutation({
    mutationFn: async (newPost: UserInviteDto) => {
      console.log(newPost);
      const response = await fetch("/api/user/create-invite", {
        method: "POST",
        body: JSON.stringify(newPost),
        headers: { "Content-type": "application/json; charset=UTF-8" },
      });

      if (!response.ok) throw new Error("Erro ao criar convite");
      return response.json();
    },
    onSuccess: () => {
      alert("convite");
    },
    onError: (error) => {
      console.error("convite n criado", error.message);
    },
  });

  return (
    <div>
      <h1>Pagina principal: {currentWorkspace}</h1>
      <Link href="/auth/logout">logout</Link>
      <Formik
        initialValues={{
          FromEmail: "",
          Email: "",
          Role: "",
        }}
        onSubmit={(values) => {
          createInviteMutation.mutate(values);
        }}
      >
        <Form className="flex flex-col">
          <TextField
            label="quem envia o convite"
            name="FromEmail"
            placeholder="email (por enquanto)"
          />

          <TextField
            label="Destino de envio"
            name="Email"
            placeholder="email tbm"
          />

          <TextField label="Cargo" name="Role" placeholder="Cargo" />

          <Button variant="outline" type="submit">
            Criar
          </Button>
        </Form>
      </Formik>
      <Button onClick={exitWorkspace}>Sair do workspace</Button>
    </div>
  );
}
