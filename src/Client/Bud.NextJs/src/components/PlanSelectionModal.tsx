import {
  Modal,
  ModalHeader,
  ModalBody,
  Button,
  Badge,
} from "@mdonangelo/bud-ds";
import { Check, ChatCircleDots } from "@phosphor-icons/react";
import styles from "./PlanSelectionModal.module.css";

interface Plan {
  id: string;
  name: string;
  description: string;
  features: string[];
  popular?: boolean;
}

const PLANS: Plan[] = [
  {
    id: "lite",
    name: "Lite",
    description: "Para empresas que estão começando com a gestão de desempenho",
    features: [
      "Plataforma completa (até 50 usuários)",
      "Templates prontos de OKRs, 1:1s e check-ins",
      "1 sessão de onboarding com especialista",
      "Suporte por e-mail",
    ],
  },
  {
    id: "pro",
    name: "Pro",
    description: "Para empresas que buscam estrutura e acompanhamento próximo",
    features: [
      "Tudo do Lite",
      "Até 200 usuários",
      "Sessões mensais de mentoring com especialista",
      "Diagnóstico de maturidade",
      "Suporte via chat prioritário",
    ],
  },
  {
    id: "master",
    name: "Master",
    description: "Para empresas que querem acelerar resultados com apoio especializado e contínuo",
    popular: true,
    features: [
      "Tudo do Pro",
      "Usuários ilimitados",
      "Sessões quinzenais com especialista dedicado",
      "Jornada personalizada de gestão",
      "Acesso à contratação de treinamentos e workshops personalizados sob demanda",
      "Utilização agentes bud",
    ],
  },
  {
    id: "enterprise",
    name: "Enterprise",
    description: "Para grandes organizações com necessidades de integração e gerenciamento específicas",
    features: [
      "Tudo do Master",
      "Plataforma, mentoring e serviços customizados",
      "Suporte dedicado",
      "Integrações avançadas",
      "Possibilidade de SLA e atendimento exclusivo",
    ],
  },
];

interface PlanSelectionModalProps {
  open: boolean;
  onClose: () => void;
}

export function PlanSelectionModal({ open, onClose }: PlanSelectionModalProps) {
  return (
    <Modal open={open} onClose={onClose} width="1100px">
      <ModalHeader
        title="Adicionar nova empresa"
        description="Para adicionar uma nova empresa, escolha um plano."
        onClose={onClose}
      />
      <ModalBody>
        <div className={styles.grid}>
          {PLANS.map((plan) => (
            <div
              key={plan.id}
              className={`${styles.card} ${plan.popular ? styles.cardPopular : ""}`}
            >
              {plan.popular && (
                <div className={styles.popularBadge}>
                  <Badge color="orange" size="sm">Popular</Badge>
                </div>
              )}
              <div className={styles.cardHeader}>
                <h3 className={styles.planName}>{plan.name}</h3>
                <p className={styles.planDescription}>{plan.description}</p>
              </div>
              <ul className={styles.featureList}>
                {plan.features.map((feature) => (
                  <li key={feature} className={styles.featureItem}>
                    <Check size={14} className={styles.featureIcon} />
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
              <div className={styles.cardFooter}>
                <Button
                  variant={plan.popular ? "primary" : "secondary"}
                  size="md"
                  leftIcon={ChatCircleDots}
                  onClick={onClose}
                >
                  Fale com um especialista
                </Button>
              </div>
            </div>
          ))}
        </div>
      </ModalBody>
    </Modal>
  );
}
