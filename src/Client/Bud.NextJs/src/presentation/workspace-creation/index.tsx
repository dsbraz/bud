import { CreateWorkspaceForm } from "./components/CreateWorkspaceForm";

export function WorkspaceCreation() {
  return (
    <div className="w-full bg-[#ffffff] p-[2rem] md:px-[4rem] flex flex-col rounded-lg">
      <h1 className="font-semibold text-2xl text-center tracking-[-0.005em] leading-[1.1] text-[#0A0A0A]">
        Criar espaço de trabalho
      </h1>
      <div className="flex flex-col mt-5">
        <CreateWorkspaceForm />
      </div>
    </div>
  );
}
