-- Seed initial predefined tags
INSERT INTO predefined_tags (id, name, slug, description, category) VALUES
-- Programming Languages
(gen_random_uuid(), 'C#', 'csharp', 'C# programming language', 'Programming Languages'),
(gen_random_uuid(), 'Python', 'python', 'Python programming language', 'Programming Languages'),
(gen_random_uuid(), 'Java', 'java', 'Java programming language', 'Programming Languages'),
(gen_random_uuid(), 'JavaScript', 'javascript', 'JavaScript programming language', 'Programming Languages'),
(gen_random_uuid(), 'TypeScript', 'typescript', 'TypeScript programming language', 'Programming Languages'),
(gen_random_uuid(), 'C++', 'cplusplus', 'C++ programming language', 'Programming Languages'),
(gen_random_uuid(), 'Go', 'go', 'Go programming language', 'Programming Languages'),
(gen_random_uuid(), 'Rust', 'rust', 'Rust programming language', 'Programming Languages'),
(gen_random_uuid(), 'PHP', 'php', 'PHP programming language', 'Programming Languages'),
(gen_random_uuid(), 'Ruby', 'ruby', 'Ruby programming language', 'Programming Languages'),

-- Web Development
(gen_random_uuid(), 'React', 'react', 'React.js library', 'Web Development'),
(gen_random_uuid(), 'Vue.js', 'vuejs', 'Vue.js framework', 'Web Development'),
(gen_random_uuid(), 'Angular', 'angular', 'Angular framework', 'Web Development'),
(gen_random_uuid(), 'Node.js', 'nodejs', 'Node.js runtime', 'Web Development'),
(gen_random_uuid(), 'HTML/CSS', 'htmlcss', 'HTML and CSS markup and styling', 'Web Development'),
(gen_random_uuid(), 'REST API', 'rest-api', 'REST API design and development', 'Web Development'),
(gen_random_uuid(), 'GraphQL', 'graphql', 'GraphQL query language', 'Web Development'),

-- Backend & Databases
(gen_random_uuid(), '.NET', 'dotnet', '.NET framework and Core', 'Backend Frameworks'),
(gen_random_uuid(), 'Spring', 'spring', 'Spring framework for Java', 'Backend Frameworks'),
(gen_random_uuid(), 'Django', 'django', 'Django framework for Python', 'Backend Frameworks'),
(gen_random_uuid(), 'Express.js', 'expressjs', 'Express.js for Node.js', 'Backend Frameworks'),
(gen_random_uuid(), 'PostgreSQL', 'postgresql', 'PostgreSQL database', 'Databases'),
(gen_random_uuid(), 'MySQL', 'mysql', 'MySQL database', 'Databases'),
(gen_random_uuid(), 'MongoDB', 'mongodb', 'MongoDB NoSQL database', 'Databases'),
(gen_random_uuid(), 'Redis', 'redis', 'Redis in-memory data store', 'Databases'),

-- DevOps & Tools
(gen_random_uuid(), 'Docker', 'docker', 'Docker containerization', 'DevOps'),
(gen_random_uuid(), 'Kubernetes', 'kubernetes', 'Kubernetes orchestration', 'DevOps'),
(gen_random_uuid(), 'Git', 'git', 'Git version control', 'Tools'),
(gen_random_uuid(), 'CI/CD', 'cicd', 'Continuous Integration/Deployment', 'DevOps'),
(gen_random_uuid(), 'AWS', 'aws', 'Amazon Web Services', 'Cloud'),
(gen_random_uuid(), 'Azure', 'azure', 'Microsoft Azure cloud', 'Cloud'),
(gen_random_uuid(), 'GCP', 'gcp', 'Google Cloud Platform', 'Cloud'),

-- Other
(gen_random_uuid(), 'Machine Learning', 'machine-learning', 'Machine learning and AI', 'Specializations'),
(gen_random_uuid(), 'Mobile Development', 'mobile-development', 'Mobile app development', 'Specializations'),
(gen_random_uuid(), 'Game Development', 'game-development', 'Game development', 'Specializations'),
(gen_random_uuid(), 'Testing', 'testing', 'Unit testing and QA', 'Practices'),
(gen_random_uuid(), 'Data Science', 'data-science', 'Data science and analytics', 'Specializations')
ON CONFLICT (slug) DO NOTHING;
