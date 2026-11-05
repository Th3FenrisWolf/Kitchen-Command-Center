export const WASH_TEXTURE_FOLDER = 'Washes'
export const WASH_CHALK_FILE = 'wash-chalk.png'
export const washBlobFile = (index: number) => `wash-blob-${index}.png`

const WASH_TEXTURE_PATH = new RegExp(
  `[\\\\/]Styles[\\\\/]Torn[\\\\/]${WASH_TEXTURE_FOLDER}[\\\\/]wash-(?:chalk|blob-\\d+)\\.png$`,
)

export const isWashTexture = (filePath: string) => WASH_TEXTURE_PATH.test(filePath)

export function inlineUnlessWashTexture(filePath: string): false | undefined {
  return isWashTexture(filePath) ? false : undefined
}
