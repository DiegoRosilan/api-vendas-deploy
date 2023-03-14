import multer, { StorageEngine } from 'multer';
import path from 'path';
import crypto from 'crypto';

interface IUploadConfig {
  driver: 's3' | 'disk';
  tmpFolder: string;
  directory: string;
  multer: {
    storage: StorageEngine;
  };
  config: {
    aws: {
      bucket: string;
    };
  };
}

//variável uploadFolder para receber os path do diretorio das imagens
//usa se '..' para voltar níveis de diretórios
const uploadFolder = path.resolve(__dirname, '..', '..', 'uploads');
const tmpFolder = path.resolve(__dirname, '..', '..', 'temp');

//exportando as confioguração do multer
export default {
  //propriedades do multer
  //directory recebe o caminho do diretório
  driver: process.env.STORAGE_DRIVER,
  directory: uploadFolder,
  tmpFolder,
  multer: {
    //storage designa onde vai ficar armazanado a imagem (diskstorage, memoryStorage)
    storage: multer.diskStorage({
      //destino do arquivo (pasta)
      destination: tmpFolder,
      //determina como vai ser nomeado o arquivo no servidor
      filename(request, file, callback) {
        //cria um hash atravas da biblioteca crypto para criar um nome
        //utilizmando o metodo randomBytes no padrão hexadecimal
        const fileHash = crypto.randomBytes(10).toString('hex');
        //compondo o nomde do arquivo com o hash e o nome original
        const filename = `${fileHash}-${file.originalname}`;
        //retorna o nome do arquivo
        callback(null, filename);
      },
    }),
  },
  config: {
    aws: {
      bucket: 'api-vendas-adsoftwares',
    },
  },
} as IUploadConfig;
