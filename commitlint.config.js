/** @type {import('@commitlint/types').UserConfig} */
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // Restrict to well-known Lexio BE scopes; add new scopes here as services land.
    'scope-enum': [
      2,
      'always',
      [
        'be-repo',
        'be-build',
        'be-shared',
        'be-infra',
        'be-config',
        'be-template',
        'be-test',
        'be-husky',
        'be-ci',
        'be-docs',
        // per-service scopes follow the pattern be-{service-name}
        'be-bb-abstractions',
        'be-bb-impls',
        'be-bb',
        'be-identity',
      ],
    ],
    // Allow any case in subject (sentence-case, lower-case both fine)
    'subject-case': [0],
  },
};
