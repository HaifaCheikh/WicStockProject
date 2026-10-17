FROM node:22-alpine
RUN apk add --no-cache git
WORKDIR /app
COPY package*.json ./
RUN npm install --omit=dev \
    && rm -rf /usr/local/lib/node_modules/npm /root/.npm
COPY . .
EXPOSE 3001
CMD ["node", "server.js"]
