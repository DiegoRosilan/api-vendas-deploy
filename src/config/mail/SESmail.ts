import nodemailer from 'nodemailer';
import aws from 'aws-sdk';
import HandlebarsMailTemplate from './HandlebarsMailTemplate';
import mailConfig from '@config/mail/mail';

//interface para permitir adicionar novos atributos
interface ITemplateVariables {
  [key: string]: string | number;
}
//interface envio de arquivos
interface IParseMailTemplate {
  file: string;
  variables: ITemplateVariables;
}

//interface Contatodo email
interface IMailContato {
  name: string;
  email: string;
}

//inetrtface de envio do email
interface ISendMail {
  to: IMailContato;
  from?: IMailContato;
  subject: string;
  templateData: IParseMailTemplate;
}

export default class EtherealMail {
  static async sendMail({
    to,
    from,
    subject,
    templateData,
  }: ISendMail): Promise<void> {
    const mailTemplate = new HandlebarsMailTemplate();

    //contruindo o transporte do email
    const transporter = nodemailer.createTransport({
      SES: new aws.SES(),
    });

    const { email, name } = mailConfig.defaults.from;

    //constuindo o corpo da mensagem
    const message = await transporter.sendMail({
      from: {
        name: from?.name || name,
        address: from?.email || email,
      },
      to: {
        name: to.name,
        address: to.email,
      },
      subject,
      html: await mailTemplate.parse(templateData),
    });
  }
}
