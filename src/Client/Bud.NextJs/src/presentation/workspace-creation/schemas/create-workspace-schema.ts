import * as Yup from "yup";

const MAX_FILE_SIZE = 900 * 1024 * 1024;
const SUPPORTED_FORMATS = ["image/jpg", "image/jpeg", "image/png"];

export const CreateWorkspaceSchema = Yup.object().shape({
  file: Yup.mixed<File>()
    .required("O arquivo é obrigatório")
    .test("fileSize", "O arquivo é muito grande (máx 900MB)", (value) => {
      return value && value.size <= MAX_FILE_SIZE;
    })
    .test("fileFormat", "Formato não suportado", (value) => {
      return value && SUPPORTED_FORMATS.includes(value.type);
    }),
  name: Yup.string().required(),
  visibility: Yup.string().required(),
});
