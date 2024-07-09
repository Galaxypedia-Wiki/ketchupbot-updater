FROM node:20-alpine AS base
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable
WORKDIR /app
COPY package*.json .
COPY tsconfig.json .
RUN pnpm install 
COPY src ./src

FROM base AS prod-deps
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --prod --frozen-lockfile

FROM base AS build
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile
RUN pnpm run build

FROM base
ENV NODE_ENV production
WORKDIR /app

COPY package*.json .
COPY src/assets/banner.txt .
COPY --from=build /app/dist /app/dist
CMD pnpm run runfrombuild -- --ships all --turrets --ship-schedule "0 * * * *" --turret-schedule "30 * * * *"