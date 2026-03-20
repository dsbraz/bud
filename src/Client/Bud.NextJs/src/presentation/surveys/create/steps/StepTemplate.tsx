import { useMemo } from "react";
import { getTemplateByType } from "../../templates/surveyTemplates";
import { Card, CardBody, Badge, Alert } from "@mdonangelo/bud-ds";
import { useSurveysData } from "@/contexts/SurveysDataContext";
import { useWizard } from "../SurveyWizardContext";
import styles from "./StepTemplate.module.css";

const CATEGORY_BADGE: Record<string, { label: string; color: "orange" | "wine" }> = {
  pesquisa: { label: "Pesquisa", color: "orange" },
  ciclo: { label: "Ciclo", color: "wine" },
};

export function StepTemplate() {
  const { templates } = useSurveysData();
  const { dispatch } = useWizard();

  const availableTemplates = useMemo(
    () => templates
      .filter((template) => !template.isArchived)
      .sort((a, b) => {
        if (a.isSystem !== b.isSystem) return a.isSystem ? -1 : 1;
        return a.name.localeCompare(b.name, "pt-BR");
      }),
    [templates],
  );

  function handleSelect(templateId: string) {
    const template = availableTemplates.find((item) => item.id === templateId);
    if (!template) return;

    dispatch({
      type: "SELECT_TEMPLATE",
      payload: {
        surveyType: template.type,
        category: template.category,
        template: {
          name: template.name,
          sections: template.sections,
          questions: template.questions,
          defaultConfig: template.defaultConfig,
        },
      },
    });
  }

  return (
    <div className={styles.step}>
      <h2 className={styles.heading}>Escolha o tipo de pesquisa</h2>
      <p className={styles.subheading}>
        Selecione um template para começar ou crie uma pesquisa personalizada
      </p>

      {availableTemplates.length === 0 && (
        <Alert variant="info" title="Nenhum template disponível">
          Crie um template em Configurações &gt; Templates de pesquisa para continuar.
        </Alert>
      )}

      <div className={styles.grid}>
        {availableTemplates.map((template) => {
          const Icon = getTemplateByType(template.type)?.icon;
          const catBadge = CATEGORY_BADGE[template.category];
          const questionCount = template.questions.length;

          return (
            <button
              key={template.id}
              type="button"
              className={styles.cardButton}
              onClick={() => handleSelect(template.id)}
            >
              <Card padding="md" shadow={false}>
                <CardBody>
                  <div className={styles.cardContent}>
                    {Icon && (
                      <div className={styles.cardIcon}>
                        <Icon size={24} />
                      </div>
                    )}

                    <h3 className={styles.cardName}>{template.name}</h3>
                    <p className={styles.cardSubtitle}>{template.subtitle}</p>

                    <div className={styles.cardBadges}>
                      {questionCount > 0 && (
                        <Badge color="neutral" size="sm">
                          {questionCount} perguntas
                        </Badge>
                      )}
                      {catBadge && (
                        <Badge color={catBadge.color} size="sm">
                          {catBadge.label}
                        </Badge>
                      )}
                    </div>
                  </div>
                </CardBody>
              </Card>
            </button>
          );
        })}
      </div>
    </div>
  );
}
