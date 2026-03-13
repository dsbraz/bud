export const generateAcronymAndColor = (workspaceName) => {
  const matches = workspaceName.match(/\b([a-zA-Zá-úÁ-Ú])/g);
  const acronym = matches ? matches.slice(0, 2).join("").toUpperCase() : "??";

  let hash = 0;
  for (let i = 0; i < workspaceName.length; i++) {
    hash = workspaceName.charCodeAt(i) + ((hash << 5) - hash);
  }

  const h = Math.abs(hash) % 360;
  const s = 50;
  const l = 55;

  const hslToHex = (h, s, l) => {
    l /= 100;
    const a = (s * Math.min(l, 1 - l)) / 100;
    const f = (n) => {
      const k = (n + h / 30) % 12;
      const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
      return Math.round(255 * color)
        .toString(16)
        .padStart(2, "0");
    };
    return `#${f(0)}${f(8)}${f(4)}`;
  };

  return {
    acronym,
    hex: hslToHex(h, s, l),
  };
};
